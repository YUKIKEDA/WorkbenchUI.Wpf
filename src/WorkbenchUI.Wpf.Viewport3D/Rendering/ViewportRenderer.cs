using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.Direct3D9;
using Silk.NET.DXGI;
using Blend = Silk.NET.Direct3D11.Blend;
using Box = Silk.NET.Direct3D11.Box;
using Buffer = System.Buffer;
using D3D9Format = Silk.NET.Direct3D9.Format;
using DxgiFormat = Silk.NET.DXGI.Format;
using PresentParameters = Silk.NET.Direct3D9.PresentParameters;

namespace WorkbenchUI.Wpf.Viewport3D.Rendering;

/// <summary>Scalar mapping parameters for contour shading (derived from <see cref="WorkbenchUI.Wpf.Controls.ColorScale"/>).</summary>
internal readonly record struct ContourSettings(
    float Min,
    float InvRange,
    bool UseLog,
    Vector4 NaNColor,
    Vector4 BelowColor,
    Vector4 AboveColor);

/// <summary>Matches HLSL cbuffer FrameConstants 1:1 (224 bytes, 16-byte aligned).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct FrameConstants
{
    public Matrix4x4 ViewProj;
    public Vector4 EyeDirection;
    public Vector4 ObjectColor;  // Solid part color / edge line color / pick pass: x = part ID
    public Vector4 ScalarParams; // x=min, y=1/range, z=contour enabled, w=log scale
    public Vector4 NaNColor;
    public Vector4 BelowColor;
    public Vector4 AboveColor;
    public Vector4 ViewportInfo; // xy=pixel size, z=point diameter (px)
    public Vector4 DeformParams; // x=deformation scale (including animation factor), y=glyph scale

    /// <summary>
    /// Triangle ID offset for the picking pass. Because SV_PrimitiveID resets per draw call,
    /// the chunk's global triangle base is added as uint (float lacks exact precision above 16.7M triangles).
    /// </summary>
    public uint PickTriangleBase;
    public uint PickPad0;
    public uint PickPad1;
    public uint PickPad2;

    public Vector4 ClipPlane;    // xyz=normalized normal, w=constant; (0,0,0,1) when disabled
}

/// <summary>Render input for a single part (mesh, selection highlight, and hover pre-highlight).</summary>
internal readonly record struct RenderItem(
    GpuMesh Mesh,
    GpuSelectionMesh? Selection,
    bool IsPartSelected,
    GpuSelectionMesh? Hover = null,
    bool IsPartHovered = false);

/// <summary>
/// AddRef'd <see cref="ID3D11Device"/> lease keeping the device alive if the renderer is disposed during
/// an asynchronous geometry build. Must be disposed on build completion or cancellation (thread-safe COM Release).
/// </summary>
/// <remarks>
/// Silk.NET's <see cref="ComPtr{T}"/> constructor calls AddRef, so wrapping a raw pointer directly would double-AddRef.
/// This class exposes the raw pointer for the duration of the lease while retaining ownership.
/// </remarks>
internal sealed unsafe class DeviceLease : IDisposable
{
    private ID3D11Device* _device;

    internal DeviceLease(ID3D11Device* device)
    {
        ((IUnknown*)device)->AddRef();
        _device = device;
    }

    /// <summary>Raw leased device pointer (null after Dispose; owned by this lease).</summary>
    public ID3D11Device* DeviceHandle => _device;

    /// <summary>
    /// Returns a temporary AddRef'd <see cref="ComPtr{T}"/>; callers must dispose it with <c>using</c>.
    /// </summary>
    public ComPtr<ID3D11Device> RentDevice() => new(_device);

    public void Dispose()
    {
        if (_device is not null)
        {
            ((IUnknown*)_device)->Release();
            _device = null;
        }
    }
}

/// <summary>
/// Direct3D 11 rendering engine.
/// <para>
/// - Hardware path: renders with MSAA in D3D11, resolves into a shared non-MSAA texture, and opens it
///   in D3D9Ex as a D3DImage back buffer (integrating natively with WPF composition without airspace issues).
/// - Software path (WARP / D3D9 unavailable): copies to a staging texture and reads pixels back into a
///   WriteableBitmap on the CPU (sufficient for on-demand rendering).
/// </para>
/// </summary>
internal sealed unsafe class ViewportRenderer : IDisposable
{
    private const int ColorMapWidth = 256;

    /// <summary>Number of floats in the section plane indicator vertex buffer (14 vertices x 6 floats).</summary>
    private const int SectionIndicatorFloatCount = 14 * 6;

    private readonly D3D11 _d3d11;
    private readonly D3DCompiler _compiler;
    private D3D9? _d3d9Api;

    private ComPtr<ID3D11Device> _device;
    private ComPtr<ID3D11DeviceContext> _context;
    private IDirect3D9Ex* _d3d9;
    private IDirect3DDevice9Ex* _d3d9Device;

    // Size-independent pipeline resources
    private ComPtr<ID3D11VertexShader> _meshVs;
    private ComPtr<ID3D11PixelShader> _meshPs;
    private ComPtr<ID3D11VertexShader> _lineVs;
    private ComPtr<ID3D11PixelShader> _linePs;
    private ComPtr<ID3D11VertexShader> _pickVs;
    private ComPtr<ID3D11PixelShader> _pickPs;
    private ComPtr<ID3D11VertexShader> _pointVs;
    private ComPtr<ID3D11PixelShader> _pointPs;
    private ComPtr<ID3D11VertexShader> _glyphVs;
    private ComPtr<ID3D11PixelShader> _glyphPs;
    private ComPtr<ID3D11InputLayout> _meshLayout;
    private ComPtr<ID3D11InputLayout> _lineLayout;
    private ComPtr<ID3D11InputLayout> _pickLayout;
    private ComPtr<ID3D11InputLayout> _pointLayout;
    private ComPtr<ID3D11InputLayout> _glyphLayout;
    private ComPtr<ID3D11Buffer> _constantBuffer;
    private ComPtr<ID3D11Buffer> _sectionIndicatorBuffer;
    private ComPtr<ID3D11Query> _frameCompleteQuery;

    /// <summary>
    /// Shared zero-displacement buffer (12 B, bound with stride 0) for undeformed meshes.
    /// Every vertex reads the same zero element, avoiding per-node displacement buffers.
    /// </summary>
    private ComPtr<ID3D11Buffer> _zeroDisplacementBuffer;
    private ComPtr<ID3D11Buffer> _glyphVertexBuffer;
    private ComPtr<ID3D11Buffer> _glyphIndexBuffer;
    private uint _glyphIndexCount;
    private ComPtr<ID3D11RasterizerState> _rasterizerState;
    private ComPtr<ID3D11DepthStencilState> _depthState;
    private ComPtr<ID3D11DepthStencilState> _highlightDepthState;
    private ComPtr<ID3D11BlendState> _alphaBlendState;
    private ComPtr<ID3D11SamplerState> _colorMapSampler;
    private ComPtr<ID3D11Texture2D> _colorMapTexture;
    private ComPtr<ID3D11ShaderResourceView> _colorMapSrv;

    // Size-dependent resources
    private ComPtr<ID3D11Texture2D> _msaaColor;
    private ComPtr<ID3D11RenderTargetView> _msaaRtv;
    private ComPtr<ID3D11Texture2D> _depthTexture;
    private ComPtr<ID3D11DepthStencilView> _depthView;
    private ComPtr<ID3D11Texture2D> _resolveTexture;
    private ComPtr<ID3D11Texture2D> _stagingTexture;
    private IDirect3DTexture9* _d3d9Texture;
    private IDirect3DSurface9* _d3d9Surface;
    private uint _sampleCount = 1;

    // Picking targets (lazily created on first pick; recreated on resize)
    private ComPtr<ID3D11Texture2D> _pickTexture;
    private ComPtr<ID3D11RenderTargetView> _pickRtv;
    private ComPtr<ID3D11Texture2D> _pickDepthTexture;
    private ComPtr<ID3D11DepthStencilView> _pickDepthView;

    // Full-frame staging texture for hover pre-highlight ID readback (lazily created on first capture)
    private ComPtr<ID3D11Texture2D> _hoverStagingTexture;
    private bool _disposed;

    public ViewportRenderer()
    {
        _d3d11 = D3D11.GetApi(null);
        _compiler = D3DCompiler.GetApi();

        try
        {
            CreateDevice();
            TryCreateD3D9();
            CreatePipeline();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Whether rendering uses the WARP software rasterizer.</summary>
    public bool IsSoftwareRendering { get; private set; }

    /// <summary>Whether presentation via D3DImage is available (otherwise falls back to WriteableBitmap).</summary>
    public bool CanUseD3DImage => _d3d9Surface is not null || (!IsSoftwareRendering && _d3d9Device is not null);

    /// <summary>IDirect3DSurface9 pointer passed to D3DImage.SetBackBuffer (hardware path only).</summary>
    public nint BackBufferSurface => (nint)_d3d9Surface;

    public int Width { get; private set; }

    public int Height { get; private set; }

    /// <summary>Number of mesh-pass chunks drawn in the most recent frame after frustum culling.</summary>
    public int LastDrawnChunkCount { get; private set; }

    /// <summary>Whether at least one LOD chunk was drawn in the most recent frame.</summary>
    public bool LastFrameUsedLod { get; private set; }

    internal ComPtr<ID3D11Device> Device => _device;

    /// <summary>Sets the target pixel dimensions and recreates size-dependent resources.</summary>
    public void Resize(int width, int height)
    {
        width = Math.Max(width, 1);
        height = Math.Max(height, 1);
        if (width == Width && height == Height && _msaaColor.Handle is not null)
        {
            return;
        }

        Width = width;
        Height = height;
        ReleaseSizedResources();

        var device = _device.Handle;

        // Check whether 4x MSAA is supported
        _sampleCount = 1;
        uint qualityLevels = 0;
        device->CheckMultisampleQualityLevels(DxgiFormat.FormatB8G8R8A8Unorm, 4, &qualityLevels);
        if (qualityLevels > 0)
        {
            _sampleCount = 4;
        }

        // MSAA color target
        var colorDesc = new Texture2DDesc
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = DxgiFormat.FormatB8G8R8A8Unorm,
            SampleDesc = new SampleDesc(_sampleCount, 0),
            Usage = Usage.Default,
            BindFlags = (uint)BindFlag.RenderTarget,
        };
        SilkMarshal.ThrowHResult(device->CreateTexture2D(&colorDesc, null, _msaaColor.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreateRenderTargetView(
            (ID3D11Resource*)_msaaColor.Handle, null, _msaaRtv.GetAddressOf()));

        // Depth-stencil buffer (matching MSAA sample count)
        var depthDesc = colorDesc with
        {
            Format = DxgiFormat.FormatD24UnormS8Uint,
            BindFlags = (uint)BindFlag.DepthStencil,
        };
        SilkMarshal.ThrowHResult(device->CreateTexture2D(&depthDesc, null, _depthTexture.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreateDepthStencilView(
            (ID3D11Resource*)_depthTexture.Handle, null, _depthView.GetAddressOf()));

        // Resolve target (non-MSAA); shared with D3D9 on the hardware path
        var resolveDesc = new Texture2DDesc
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = DxgiFormat.FormatB8G8R8A8Unorm,
            SampleDesc = new SampleDesc(1, 0),
            Usage = Usage.Default,
            BindFlags = (uint)BindFlag.RenderTarget,
            MiscFlags = _d3d9Device is not null ? (uint)ResourceMiscFlag.Shared : 0u,
        };
        SilkMarshal.ThrowHResult(device->CreateTexture2D(&resolveDesc, null, _resolveTexture.GetAddressOf()));

        if (_d3d9Device is not null)
        {
            OpenSharedSurfaceOnD3D9(width, height);
        }

        if (_d3d9Surface is null)
        {
            // Software path: staging texture for CPU readback
            var stagingDesc = resolveDesc with
            {
                Usage = Usage.Staging,
                BindFlags = 0,
                MiscFlags = 0,
                CPUAccessFlags = (uint)CpuAccessFlag.Read,
            };
            SilkMarshal.ThrowHResult(device->CreateTexture2D(&stagingDesc, null, _stagingTexture.GetAddressOf()));
        }
    }

    /// <summary>Rebuilds the 1D contour color-map texture from sampled RGBA bytes (4 bytes/texel x 256).</summary>
    public void SetColorMap(ReadOnlySpan<byte> rgba)
    {
        if (rgba.Length != ColorMapWidth * 4)
        {
            throw new ArgumentException($"Color map requires {ColorMapWidth} texels of RGBA data.", nameof(rgba));
        }

        _colorMapSrv.Dispose();
        _colorMapTexture.Dispose();
        _colorMapSrv = default;
        _colorMapTexture = default;

        var desc = new Texture2DDesc
        {
            Width = ColorMapWidth,
            Height = 1,
            MipLevels = 1,
            ArraySize = 1,
            Format = DxgiFormat.FormatR8G8B8A8Unorm,
            SampleDesc = new SampleDesc(1, 0),
            Usage = Usage.Immutable,
            BindFlags = (uint)BindFlag.ShaderResource,
        };

        fixed (byte* pData = rgba)
        {
            var init = new SubresourceData
            {
                PSysMem = pData,
                SysMemPitch = ColorMapWidth * 4,
            };
            SilkMarshal.ThrowHResult(_device.Handle->CreateTexture2D(&desc, &init, _colorMapTexture.GetAddressOf()));
        }

        SilkMarshal.ThrowHResult(_device.Handle->CreateShaderResourceView(
            (ID3D11Resource*)_colorMapTexture.Handle, null, _colorMapSrv.GetAddressOf()));
    }

    /// <summary>Renders the full scene and resolves MSAA into the shared output texture.</summary>
    public void Render(
        IReadOnlyList<RenderItem> items,
        in Matrix4x4 viewProj,
        Vector3 eyeDirection,
        Vector4 background,
        in ContourSettings contour,
        bool contoursEnabled,
        Vector4 edgeColor,
        Vector4 highlightColor,
        Vector4 nodeColor,
        float nodePointSizePixels,
        Vector4 hoverColor,
        float deformationScale,
        float glyphScale,
        bool showUndeformedWireframe,
        Vector4 undeformedColor,
        Vector4 clipPlane,
        float[]? sectionIndicatorVertices,
        Vector4 sectionFillColor,
        Vector4 sectionLineColor,
        bool useLod = false)
    {
        if (_msaaRtv.Handle is null)
        {
            return;
        }

        var ctx = _context.Handle;

        // Frustum culling: skip chunks whose AABB lies completely outside the view frustum.
        // Expand AABB conservatively by max displacement * |scale| when deformation is active.
        Span<Vector4> frustum = stackalloc Vector4[ViewportCulling.PlaneCount];
        ViewportCulling.ExtractFrustumPlanes(in viewProj, frustum);
        var drawnChunks = 0;
        var usedLod = false;

        var bg = stackalloc float[4] { background.X, background.Y, background.Z, background.W };
        ctx->ClearRenderTargetView(_msaaRtv.Handle, bg);
        ctx->ClearDepthStencilView(_depthView.Handle, (uint)ClearFlag.Depth, 1.0f, 0);

        var rtv = _msaaRtv.Handle;
        ctx->OMSetRenderTargets(1, &rtv, _depthView.Handle);

        var viewport = new Silk.NET.Direct3D11.Viewport(0, 0, Width, Height, 0.0f, 1.0f);
        ctx->RSSetViewports(1, &viewport);
        ctx->RSSetState(_rasterizerState.Handle);
        ctx->OMSetDepthStencilState(_depthState.Handle, 0);

        var cb = _constantBuffer.Handle;
        ctx->VSSetConstantBuffers(0, 1, &cb);
        ctx->PSSetConstantBuffers(0, 1, &cb);

        var srv = _colorMapSrv.Handle;
        var sampler = _colorMapSampler.Handle;
        ctx->PSSetShaderResources(0, 1, &srv);
        ctx->PSSetSamplers(0, 1, &sampler);

        var constants = new FrameConstants
        {
            ViewProj = viewProj,
            EyeDirection = new Vector4(eyeDirection, 0.0f),
            ScalarParams = new Vector4(contour.Min, contour.InvRange, 0.0f, contour.UseLog ? 1.0f : 0.0f),
            NaNColor = contour.NaNColor,
            BelowColor = contour.BelowColor,
            AboveColor = contour.AboveColor,
            ViewportInfo = new Vector4(Width, Height, nodePointSizePixels, 0.0f),
            DeformParams = new Vector4(deformationScale, glyphScale, 0.0f, 0.0f),
            ClipPlane = ViewportSection.DisabledClip,
        };

        // Pass 1: Opaque meshes followed by transparent meshes, drawn per chunk (using LOD chunks when active)
        foreach (var transparentPass in (ReadOnlySpan<bool>)[false, true])
        {
            foreach (var item in items)
            {
                var mesh = item.Mesh;
                if (mesh.IsTransparent != transparentPass)
                {
                    continue;
                }

                ctx->OMSetBlendState(transparentPass ? _alphaBlendState.Handle : null, null, 0xFFFFFFFF);

                constants.ObjectColor = mesh.Color;
                constants.ScalarParams.Z = contoursEnabled && mesh.HasScalars && _colorMapSrv.Handle is not null ? 1.0f : 0.0f;
                constants.ClipPlane = mesh.IsClippable ? clipPlane : ViewportSection.DisabledClip;
                UploadConstants(in constants);

                ctx->IASetInputLayout(_meshLayout.Handle);
                ctx->VSSetShader(_meshVs.Handle, null, 0);
                ctx->PSSetShader(_meshPs.Handle, null, 0);
                ctx->IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyTrianglelist);

                var meshUseLod = useLod && mesh.HasLod;
                usedLod |= meshUseLod;
                var expand = mesh.MaxDisplacementMagnitude * MathF.Abs(deformationScale);
                foreach (var chunk in meshUseLod ? mesh.LodChunks : mesh.Chunks)
                {
                    if (!ViewportCulling.IntersectsFrustum(frustum, chunk.BoundsMin, chunk.BoundsMax, expand))
                    {
                        continue;
                    }

                    BindVertexBuffer(ctx, chunk);
                    ctx->IASetIndexBuffer(chunk.TriangleIndexBufferHandle, DxgiFormat.FormatR32Uint, 0);
                    ctx->DrawIndexed(chunk.TriangleIndexCount, 0, 0);
                    drawnChunks++;
                }
            }
        }

        // Pass 2: Edge overlay (opaque, depth-tested with slight forward depth bias).
        // Skip edges, glyphs, and undeformed wireframe while interactive LOD is active on a mesh.
        ctx->OMSetBlendState(null, null, 0xFFFFFFFF);
        foreach (var item in items)
        {
            var mesh = item.Mesh;
            if (!mesh.ShowEdges || mesh.EdgesSkipped || (useLod && mesh.HasLod))
            {
                continue;
            }

            constants.ObjectColor = edgeColor;
            constants.ScalarParams.Z = 0.0f;
            constants.ClipPlane = mesh.IsClippable ? clipPlane : ViewportSection.DisabledClip;
            UploadConstants(in constants);

            ctx->IASetInputLayout(_lineLayout.Handle);
            ctx->VSSetShader(_lineVs.Handle, null, 0);
            ctx->PSSetShader(_linePs.Handle, null, 0);
            ctx->IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyLinelist);
            var expand = mesh.MaxDisplacementMagnitude * MathF.Abs(deformationScale);
            foreach (var chunk in mesh.Chunks)
            {
                if (chunk.EdgeIndexCount == 0
                    || !ViewportCulling.IntersectsFrustum(frustum, chunk.BoundsMin, chunk.BoundsMax, expand))
                {
                    continue;
                }

                BindVertexBuffer(ctx, chunk);
                ctx->IASetIndexBuffer(chunk.EdgeIndexBufferHandle, DxgiFormat.FormatR32Uint, 0);
                ctx->DrawIndexed(chunk.EdgeIndexCount, 0, 0);
            }
        }

        // Pass 2.2: Vector glyphs via DrawIndexedInstanced on the unit-arrow prototype.
        // Rendered as opaque geometry with depth writes so arrows occlude properly with meshes.
        if (glyphScale > 0.0f && _glyphIndexCount > 0)
        {
            var glyphBuffers = stackalloc ID3D11Buffer*[2] { _glyphVertexBuffer.Handle, null };
            var glyphStrides = stackalloc uint[2] { 24, GpuMesh.GlyphInstanceStride };
            var glyphOffsets = stackalloc uint[2] { 0, 0 };
            foreach (var item in items)
            {
                var mesh = item.Mesh;
                if (mesh.GlyphInstanceCount == 0 || mesh.GlyphBuffers.Count == 0 || (useLod && mesh.HasLod))
                {
                    continue;
                }

                constants.ScalarParams.Z = 0.0f;
                constants.ClipPlane = mesh.IsClippable ? clipPlane : ViewportSection.DisabledClip;
                UploadConstants(in constants);

                ctx->IASetInputLayout(_glyphLayout.Handle);
                ctx->VSSetShader(_glyphVs.Handle, null, 0);
                ctx->PSSetShader(_glyphPs.Handle, null, 0);
                ctx->IASetIndexBuffer(_glyphIndexBuffer.Handle, DxgiFormat.FormatR32Uint, 0);
                ctx->IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyTrianglelist);

                // Large vector fields split across multiple instance buffers
                foreach (var (buffer, instanceCount, _) in mesh.GlyphBuffers)
                {
                    glyphBuffers[1] = buffer.Handle;
                    ctx->IASetVertexBuffers(0, 2, glyphBuffers, glyphStrides, glyphOffsets);
                    ctx->DrawIndexedInstanced(_glyphIndexCount, instanceCount, 0, 0, 0);
                }
            }
        }

        // Pass 2.5: Undeformed wireframe overlay (DeformParams.x = 0) drawn semi-transparently without depth writes
        if (showUndeformedWireframe)
        {
            ctx->OMSetBlendState(_alphaBlendState.Handle, null, 0xFFFFFFFF);
            ctx->OMSetDepthStencilState(_highlightDepthState.Handle, 0);

            constants.DeformParams.X = 0.0f;
            foreach (var item in items)
            {
                var mesh = item.Mesh;
                if (mesh.EdgesSkipped || (useLod && mesh.HasLod))
                {
                    continue;
                }

                constants.ObjectColor = undeformedColor;
                constants.ScalarParams.Z = 0.0f;
                // Clip the undeformed geometry against the same section plane (with DeformParams.x = 0, shader evaluates undeformed coordinates)
                constants.ClipPlane = mesh.IsClippable ? clipPlane : ViewportSection.DisabledClip;
                UploadConstants(in constants);

                ctx->IASetInputLayout(_lineLayout.Handle);
                ctx->VSSetShader(_lineVs.Handle, null, 0);
                ctx->PSSetShader(_linePs.Handle, null, 0);
                ctx->IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyLinelist);
                foreach (var chunk in mesh.Chunks)
                {
                    // Undeformed rendering requires no displacement AABB margin
                    if (chunk.EdgeIndexCount == 0
                        || !ViewportCulling.IntersectsFrustum(frustum, chunk.BoundsMin, chunk.BoundsMax))
                    {
                        continue;
                    }

                    BindVertexBuffer(ctx, chunk);
                    ctx->IASetIndexBuffer(chunk.EdgeIndexBufferHandle, DxgiFormat.FormatR32Uint, 0);
                    ctx->DrawIndexed(chunk.EdgeIndexCount, 0, 0);
                }
            }

            constants.DeformParams.X = deformationScale;
            ctx->OMSetBlendState(null, null, 0xFFFFFFFF);
            ctx->OMSetDepthStencilState(_depthState.Handle, 0);
        }

        // Pass 3: Selected face highlights (semi-transparent overlay, no depth writes).
        // Whole-part selection draws all mesh chunks directly; face selection uses GpuSelectionMesh's standalone buffer.
        // Hover pre-highlight uses the same pipeline in an earlier pass with a lighter tint before selection.
        ctx->OMSetBlendState(_alphaBlendState.Handle, null, 0xFFFFFFFF);
        ctx->OMSetDepthStencilState(_highlightDepthState.Handle, 0);
        var faceBuffers = stackalloc ID3D11Buffer*[2];
        var faceStrides = stackalloc uint[2] { GpuSelectionMesh.FaceVertexStride, GpuSelectionMesh.FaceVertexStride };
        var faceOffsets = stackalloc uint[2] { 0, 12 };
        foreach (var hoverPass in (ReadOnlySpan<bool>)[true, false])
        {
            foreach (var item in items)
            {
                var overlay = hoverPass ? item.Hover : item.Selection;
                var isPartPass = hoverPass ? item.IsPartHovered : item.IsPartSelected;
                var isFacePass = overlay is { FaceVertexCount: > 0 };
                if (!isPartPass && !isFacePass)
                {
                    continue;
                }

                constants.ObjectColor = hoverPass ? hoverColor : highlightColor;
                constants.ScalarParams.Z = 0.0f;
                constants.ClipPlane = item.Mesh.IsClippable ? clipPlane : ViewportSection.DisabledClip;
                UploadConstants(in constants);

                ctx->IASetInputLayout(_lineLayout.Handle);
                ctx->VSSetShader(_lineVs.Handle, null, 0);
                ctx->PSSetShader(_linePs.Handle, null, 0);
                ctx->IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyTrianglelist);

                if (isPartPass)
                {
                    // When LOD is active, draw part highlights on LOD chunks as well to stay fast
                    var mesh = item.Mesh;
                    var meshUseLod = useLod && mesh.HasLod;
                    var expand = mesh.MaxDisplacementMagnitude * MathF.Abs(deformationScale);
                    foreach (var chunk in meshUseLod ? mesh.LodChunks : mesh.Chunks)
                    {
                        if (!ViewportCulling.IntersectsFrustum(frustum, chunk.BoundsMin, chunk.BoundsMax, expand))
                        {
                            continue;
                        }

                        BindVertexBuffer(ctx, chunk);
                        ctx->IASetIndexBuffer(chunk.TriangleIndexBufferHandle, DxgiFormat.FormatR32Uint, 0);
                        ctx->DrawIndexed(chunk.TriangleIndexCount, 0, 0);
                    }
                }
                else
                {
                    // Double-bind the same buffer at slot 0 (offset 0) and slot 1 (offset 12) to satisfy the 2-slot line shader layout
                    faceBuffers[0] = overlay!.FaceVertexBufferHandle;
                    faceBuffers[1] = overlay.FaceVertexBufferHandle;
                    ctx->IASetVertexBuffers(0, 2, faceBuffers, faceStrides, faceOffsets);
                    ctx->Draw(overlay.FaceVertexCount, 0);
                }
            }
        }

        // Pass 4: Selected node points (circular points, depth-tested without depth writes);
        // hover node is drawn first in a lighter tint.
        foreach (var hoverPass in (ReadOnlySpan<bool>)[true, false])
        {
            foreach (var item in items)
            {
                var overlay = hoverPass ? item.Hover : item.Selection;
                if (overlay is not { NodeVertexCount: > 0 } points)
                {
                    continue;
                }

                constants.ObjectColor = hoverPass
                    ? hoverColor with { W = MathF.Min(hoverColor.W * 2.0f, 0.9f) }
                    : nodeColor;
                constants.ClipPlane = item.Mesh.IsClippable ? clipPlane : ViewportSection.DisabledClip;
                UploadConstants(in constants);

                var vb = points.NodeVertexBufferHandle;
                var stride = GpuSelectionMesh.PointVertexStride;
                uint offset = 0;
                ctx->IASetVertexBuffers(0, 1, &vb, &stride, &offset);
                ctx->IASetInputLayout(_pointLayout.Handle);
                ctx->VSSetShader(_pointVs.Handle, null, 0);
                ctx->PSSetShader(_pointPs.Handle, null, 0);
                ctx->IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyTrianglelist);
                ctx->Draw(points.NodeVertexCount, 0);
            }
        }

        // Pass 5: Section plane indicator (semi-transparent quad + outline), depth-tested without depth writes
        if (sectionIndicatorVertices is { Length: >= SectionIndicatorFloatCount })
        {
            ctx->OMSetBlendState(_alphaBlendState.Handle, null, 0xFFFFFFFF);
            ctx->OMSetDepthStencilState(_highlightDepthState.Handle, 0);
            RenderSectionIndicator(ctx, sectionIndicatorVertices, sectionFillColor, sectionLineColor, ref constants);
        }

        ctx->OMSetBlendState(null, null, 0xFFFFFFFF);
        ctx->OMSetDepthStencilState(_depthState.Handle, 0);

        // Resolve MSAA -> non-MSAA output texture
        if (_sampleCount > 1)
        {
            ctx->ResolveSubresource(
                (ID3D11Resource*)_resolveTexture.Handle, 0,
                (ID3D11Resource*)_msaaColor.Handle, 0,
                DxgiFormat.FormatB8G8R8A8Unorm);
        }
        else
        {
            ctx->CopyResource((ID3D11Resource*)_resolveTexture.Handle, (ID3D11Resource*)_msaaColor.Handle);
        }

        ctx->Flush();
        WaitForGpuCompletion();

        LastDrawnChunkCount = drawnChunks;
        LastFrameUsedLod = usedLod;
    }

    /// <summary>
    /// Waits for submitted GPU commands to complete via an event query.
    /// On the D3DImage path, WPF's composition thread may copy the shared surface at any time;
    /// Flush alone can leave stale pixels visible on large scenes until the next on-demand render.
    /// Waiting ensures rendered pixels are finalized before AddDirtyRect.
    /// </summary>
    private void WaitForGpuCompletion()
    {
        if (_frameCompleteQuery.Handle is null)
        {
            return;
        }

        var ctx = _context.Handle;
        ctx->End((ID3D11Asynchronous*)_frameCompleteQuery.Handle);

        // GetData returns S_OK (0) when complete, S_FALSE (1) while pending; guard against hangs with a timeout
        var deadline = Environment.TickCount64 + 1000;
        while (Environment.TickCount64 < deadline)
        {
            var hr = ctx->GetData((ID3D11Asynchronous*)_frameCompleteQuery.Handle, null, 0, 0);
            if (hr != 1)
            {
                return; // S_OK (complete) or failure (e.g., device lost, handled by caller)
            }

            Thread.SpinWait(64);
        }
    }

    // ================= GPU ID picking =================

    /// <summary>
    /// Picks a single pixel in the ID pass, returning (meshIndex, triangleIndex) into <paramref name="meshes"/>, or null.
    /// </summary>
    public (int MeshIndex, int TriangleIndex)? PickPixel(
        IReadOnlyList<GpuMesh> meshes, in Matrix4x4 viewProj, int x, int y, float deformationScale,
        Vector4 clipPlane)
    {
        var region = PickRegion(meshes, in viewProj, x, y, 1, 1, deformationScale, clipPlane);
        foreach (var (meshIndex, triangles) in region)
        {
            foreach (var triangle in triangles)
            {
                return (meshIndex, triangle);
            }
        }

        return null;
    }

    /// <summary>
    /// Picks all visible triangles within a screen rectangle (meshIndex -> set of global triangle indices).
    /// </summary>
    public Dictionary<int, HashSet<int>> PickRegion(
        IReadOnlyList<GpuMesh> meshes, in Matrix4x4 viewProj, int x, int y, int width, int height,
        float deformationScale, Vector4 clipPlane)
    {
        var result = new Dictionary<int, HashSet<int>>();

        // Clamp to viewport bounds
        var x0 = Math.Max(x, 0);
        var y0 = Math.Max(y, 0);
        var x1 = Math.Min(x + width, Width);
        var y1 = Math.Min(y + height, Height);
        if (x0 >= x1 || y0 >= y1 || meshes.Count == 0 || _msaaRtv.Handle is null)
        {
            return result;
        }

        RenderIdPass(meshes, in viewProj, deformationScale, clipPlane);

        var regionWidth = x1 - x0;
        var regionHeight = y1 - y0;
        var device = _device.Handle;
        var ctx = _context.Handle;

        // Allocate a region-sized staging texture on demand (interactive picking rate makes this negligible)
        var stagingDesc = new Texture2DDesc
        {
            Width = (uint)regionWidth,
            Height = (uint)regionHeight,
            MipLevels = 1,
            ArraySize = 1,
            Format = DxgiFormat.FormatR32G32Uint,
            SampleDesc = new SampleDesc(1, 0),
            Usage = Usage.Staging,
            CPUAccessFlags = (uint)CpuAccessFlag.Read,
        };

        ComPtr<ID3D11Texture2D> staging = default;
        SilkMarshal.ThrowHResult(device->CreateTexture2D(&stagingDesc, null, staging.GetAddressOf()));
        try
        {
            var box = new Box((uint)x0, (uint)y0, 0, (uint)x1, (uint)y1, 1);
            ctx->CopySubresourceRegion(
                (ID3D11Resource*)staging.Handle, 0, 0, 0, 0,
                (ID3D11Resource*)_pickTexture.Handle, 0, &box);

            var mapped = default(MappedSubresource);
            SilkMarshal.ThrowHResult(ctx->Map((ID3D11Resource*)staging.Handle, 0, Map.Read, 0, &mapped));
            try
            {
                for (var row = 0; row < regionHeight; row++)
                {
                    var pRow = (uint*)((byte*)mapped.PData + row * (int)mapped.RowPitch);
                    for (var col = 0; col < regionWidth; col++)
                    {
                        var part = pRow[col * 2];
                        if (part == 0)
                        {
                            continue; // Background
                        }

                        var meshIndex = (int)part - 1;
                        if (meshIndex >= meshes.Count)
                        {
                            continue;
                        }

                        var triangle = (int)pRow[col * 2 + 1];
                        if (!result.TryGetValue(meshIndex, out var set))
                        {
                            set = [];
                            result[meshIndex] = set;
                        }

                        set.Add(triangle);
                    }
                }
            }
            finally
            {
                ctx->Unmap((ID3D11Resource*)staging.Handle, 0);
            }
        }
        finally
        {
            staging.Dispose();
        }

        return result;
    }

    /// <summary>
    /// Reads back the full-resolution ID pass for hover pre-highlighting into a row-major buffer of
    /// two uints per pixel (R = part ID + 1, G = global triangle index). Reuses <paramref name="destination"/>
    /// when its length matches so subsequent pointer moves require zero GPU work or allocations.
    /// </summary>
    public bool CaptureIdBuffer(
        IReadOnlyList<GpuMesh> meshes, in Matrix4x4 viewProj, float deformationScale,
        Vector4 clipPlane, ref uint[]? destination)
    {
        if (meshes.Count == 0 || _msaaRtv.Handle is null || Width <= 0 || Height <= 0)
        {
            return false;
        }

        RenderIdPass(meshes, in viewProj, deformationScale, clipPlane);

        var device = _device.Handle;
        var ctx = _context.Handle;

        if (_hoverStagingTexture.Handle is null)
        {
            var stagingDesc = new Texture2DDesc
            {
                Width = (uint)Width,
                Height = (uint)Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DxgiFormat.FormatR32G32Uint,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Staging,
                CPUAccessFlags = (uint)CpuAccessFlag.Read,
            };
            SilkMarshal.ThrowHResult(device->CreateTexture2D(&stagingDesc, null, _hoverStagingTexture.GetAddressOf()));
        }

        ctx->CopyResource((ID3D11Resource*)_hoverStagingTexture.Handle, (ID3D11Resource*)_pickTexture.Handle);

        var required = Width * Height * 2;
        if (destination is null || destination.Length != required)
        {
            destination = new uint[required];
        }

        var mapped = default(MappedSubresource);
        SilkMarshal.ThrowHResult(ctx->Map((ID3D11Resource*)_hoverStagingTexture.Handle, 0, Map.Read, 0, &mapped));
        try
        {
            fixed (uint* pDest = destination)
            {
                var rowBytes = Width * 2 * sizeof(uint);
                for (var row = 0; row < Height; row++)
                {
                    Buffer.MemoryCopy(
                        (byte*)mapped.PData + row * (int)mapped.RowPitch,
                        (byte*)pDest + row * rowBytes,
                        rowBytes,
                        rowBytes);
                }
            }
        }
        finally
        {
            ctx->Unmap((ID3D11Resource*)_hoverStagingTexture.Handle, 0);
        }

        return true;
    }

    /// <summary>Renders the ID pass (R = part ID + 1, G = global triangle index; non-MSAA with dedicated depth).</summary>
    private void RenderIdPass(
        IReadOnlyList<GpuMesh> meshes, in Matrix4x4 viewProj, float deformationScale, Vector4 clipPlane)
    {
        EnsurePickTargets();

        var ctx = _context.Handle;

        // Zero-clearing an integer RTV via float zeros produces 0 in uint representation
        var zero = stackalloc float[4];
        ctx->ClearRenderTargetView(_pickRtv.Handle, zero);
        ctx->ClearDepthStencilView(_pickDepthView.Handle, (uint)ClearFlag.Depth, 1.0f, 0);

        var rtv = _pickRtv.Handle;
        ctx->OMSetRenderTargets(1, &rtv, _pickDepthView.Handle);

        var viewport = new Silk.NET.Direct3D11.Viewport(0, 0, Width, Height, 0.0f, 1.0f);
        ctx->RSSetViewports(1, &viewport);
        ctx->RSSetState(_rasterizerState.Handle);
        ctx->OMSetDepthStencilState(_depthState.Handle, 0);
        ctx->OMSetBlendState(null, null, 0xFFFFFFFF);

        var cb = _constantBuffer.Handle;
        ctx->VSSetConstantBuffers(0, 1, &cb);
        ctx->PSSetConstantBuffers(0, 1, &cb);

        var constants = new FrameConstants
        {
            ViewProj = viewProj,
            DeformParams = new Vector4(deformationScale, 0.0f, 0.0f, 0.0f),
            ClipPlane = ViewportSection.DisabledClip,
        };

        ctx->IASetInputLayout(_pickLayout.Handle);
        ctx->VSSetShader(_pickVs.Handle, null, 0);
        ctx->PSSetShader(_pickPs.Handle, null, 0);
        ctx->IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyTrianglelist);

        // Frustum-cull chunks in the ID pass as well (chunks outside the frustum never affect visible pixels).
        // Picking and probing always draw full-resolution meshes even while interactive LOD is active.
        Span<Vector4> frustum = stackalloc Vector4[ViewportCulling.PlaneCount];
        ViewportCulling.ExtractFrustumPlanes(in viewProj, frustum);

        for (var i = 0; i < meshes.Count; i++)
        {
            var mesh = meshes[i];
            constants.ObjectColor = new Vector4(i + 1, 0.0f, 0.0f, 0.0f);
            // Apply the same section clip as the display pass so clipped geometry is unpickable
            constants.ClipPlane = mesh.IsClippable ? clipPlane : ViewportSection.DisabledClip;

            var expand = mesh.MaxDisplacementMagnitude * MathF.Abs(deformationScale);
            foreach (var chunk in mesh.Chunks)
            {
                if (!ViewportCulling.IntersectsFrustum(frustum, chunk.BoundsMin, chunk.BoundsMax, expand))
                {
                    continue;
                }

                // SV_PrimitiveID resets per draw call, so pass the chunk's global triangle base via cbuffer
                constants.PickTriangleBase = chunk.TriangleBase;
                UploadConstants(in constants);

                BindVertexBuffer(ctx, chunk);
                ctx->IASetIndexBuffer(chunk.TriangleIndexBufferHandle, DxgiFormat.FormatR32Uint, 0);
                ctx->DrawIndexed(chunk.TriangleIndexCount, 0, 0);
            }
        }
    }

    private void EnsurePickTargets()
    {
        if (_pickTexture.Handle is not null)
        {
            return;
        }

        var device = _device.Handle;

        var pickDesc = new Texture2DDesc
        {
            Width = (uint)Width,
            Height = (uint)Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = DxgiFormat.FormatR32G32Uint,
            SampleDesc = new SampleDesc(1, 0),
            Usage = Usage.Default,
            BindFlags = (uint)BindFlag.RenderTarget,
        };
        SilkMarshal.ThrowHResult(device->CreateTexture2D(&pickDesc, null, _pickTexture.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreateRenderTargetView(
            (ID3D11Resource*)_pickTexture.Handle, null, _pickRtv.GetAddressOf()));

        var depthDesc = pickDesc with
        {
            Format = DxgiFormat.FormatD24UnormS8Uint,
            BindFlags = (uint)BindFlag.DepthStencil,
        };
        SilkMarshal.ThrowHResult(device->CreateTexture2D(&depthDesc, null, _pickDepthTexture.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreateDepthStencilView(
            (ID3D11Resource*)_pickDepthTexture.Handle, null, _pickDepthView.GetAddressOf()));
    }

    /// <summary>
    /// Software path: copies the resolved frame via staging texture into a CPU BGRA buffer with the specified stride.
    /// </summary>
    public void ReadPixels(nint destination, int destStride)
    {
        if (_stagingTexture.Handle is null)
        {
            return;
        }

        var ctx = _context.Handle;
        ctx->CopyResource((ID3D11Resource*)_stagingTexture.Handle, (ID3D11Resource*)_resolveTexture.Handle);

        var mapped = default(MappedSubresource);
        SilkMarshal.ThrowHResult(ctx->Map((ID3D11Resource*)_stagingTexture.Handle, 0, Map.Read, 0, &mapped));
        try
        {
            var rowBytes = Math.Min((int)mapped.RowPitch, Math.Min(destStride, Width * 4));
            for (var y = 0; y < Height; y++)
            {
                Buffer.MemoryCopy(
                    (byte*)mapped.PData + y * (int)mapped.RowPitch,
                    (byte*)destination + y * destStride,
                    destStride,
                    rowBytes);
            }
        }
        finally
        {
            ctx->Unmap((ID3D11Resource*)_stagingTexture.Handle, 0);
        }
    }

    public GpuMesh? CreateMesh(
        ViewportMesh mesh, double originX, double originY, double originZ,
        int edgeExtractionLimit = int.MaxValue,
        int interactiveLodThreshold = int.MaxValue,
        Bounds3D meshBounds = default) =>
        GpuMesh.Create(_device, mesh, originX, originY, originZ, edgeExtractionLimit,
            interactiveLodThreshold, meshBounds);

    /// <summary>
    /// Acquires an AddRef'd device lease for asynchronous geometry builds, keeping the device valid
    /// even if the renderer is disposed mid-build.
    /// </summary>
    public DeviceLease AcquireDeviceLease() => new(_device.Handle);

    /// <summary>Replaces only the displacement buffer (for transient playback frame updates).</summary>
    public void UpdateMeshDisplacements(GpuMesh mesh, double[]? displacements) =>
        mesh.UpdateDisplacements(_device, _context, displacements);

    /// <summary>Replaces only vertex scalars (for contour / voxel filling playback).</summary>
    public void UpdateMeshScalars(GpuMesh mesh, double[]? scalars) =>
        mesh.UpdateScalars(_device, _context, scalars);

    /// <summary>Replaces glyph instance buffers.</summary>
    public void UpdateMeshGlyphs(GpuMesh mesh, float[] instanceData, int instanceCount) =>
        mesh.UpdateGlyphInstances(_device, _context, instanceData, instanceCount);

    public GpuSelectionMesh? CreateSelectionMesh(
        ViewportMesh mesh,
        IReadOnlyCollection<int> selectedFaces,
        IReadOnlyCollection<int> selectedNodes,
        double originX, double originY, double originZ) =>
        GpuSelectionMesh.Create(_device, mesh, selectedFaces, selectedNodes, originX, originY, originZ);

    // ================= Initialization =================

    private void CreateDevice()
    {
        // BGRA support is required for D3DImage / D3D9 shared surfaces
        const uint flags = (uint)CreateDeviceFlag.BgraSupport;

        var hr = _d3d11.CreateDevice(
            default(ComPtr<IDXGIAdapter>),
            D3DDriverType.Hardware,
            Software: 0,
            flags,
            null,
            0,
            D3D11.SdkVersion,
            ref _device,
            null,
            ref _context);

        if (HResult.IndicatesFailure(hr))
        {
            // WARP software fallback for headless / RDP environments without a hardware GPU
            SilkMarshal.ThrowHResult(_d3d11.CreateDevice(
                default(ComPtr<IDXGIAdapter>),
                D3DDriverType.Warp,
                Software: 0,
                flags,
                null,
                0,
                D3D11.SdkVersion,
                ref _device,
                null,
                ref _context));
            IsSoftwareRendering = true;
        }
    }

    private void TryCreateD3D9()
    {
        if (IsSoftwareRendering)
        {
            // WARP shared textures cannot be opened by hardware D3D9; use CPU readback instead
            return;
        }

        try
        {
            _d3d9Api = D3D9.GetApi(null);

            IDirect3D9Ex* d3d9 = null;
            SilkMarshal.ThrowHResult(_d3d9Api.Direct3DCreate9Ex(D3D9.SdkVersion, &d3d9));
            _d3d9 = d3d9;

            var hwnd = GetDesktopWindow();
            var pp = new PresentParameters
            {
                Windowed = 1,
                SwapEffect = Swapeffect.Discard,
                BackBufferFormat = D3D9Format.Unknown,
                BackBufferWidth = 1,
                BackBufferHeight = 1,
                BackBufferCount = 1,
                HDeviceWindow = hwnd,
            };

            // 0x46 = HARDWARE_VERTEXPROCESSING | MULTITHREADED | FPU_PRESERVE
            const uint behaviorFlags = 0x40 | 0x04 | 0x02;

            IDirect3DDevice9Ex* device = null;
            SilkMarshal.ThrowHResult(_d3d9->CreateDeviceEx(
                0, Devtype.Hal, hwnd, behaviorFlags, &pp, null, &device));
            _d3d9Device = device;
        }
        catch
        {
            // Fall back to CPU readback if D3D9 initialization fails
            ReleaseD3D9();
        }
    }

    private void OpenSharedSurfaceOnD3D9(int width, int height)
    {
        try
        {
            // Retrieve the shared handle from the D3D11 resolve texture
            IDXGIResource* dxgiResource = null;
            var iid = IDXGIResource.Guid;
            SilkMarshal.ThrowHResult(
                ((IUnknown*)_resolveTexture.Handle)->QueryInterface(&iid, (void**)&dxgiResource));

            void* sharedHandle = null;
            var hr = dxgiResource->GetSharedHandle(&sharedHandle);
            dxgiResource->Release();
            SilkMarshal.ThrowHResult(hr);

            // Open the same texture on D3D9Ex (A8R8G8B8 = B8G8R8A8Unorm)
            const uint usageRenderTarget = 0x00000001;
            IDirect3DTexture9* texture = null;
            SilkMarshal.ThrowHResult(_d3d9Device->CreateTexture(
                (uint)width, (uint)height, 1, usageRenderTarget,
                D3D9Format.A8R8G8B8, Pool.Default, &texture, &sharedHandle));
            _d3d9Texture = texture;

            IDirect3DSurface9* surface = null;
            SilkMarshal.ThrowHResult(_d3d9Texture->GetSurfaceLevel(0, &surface));
            _d3d9Surface = surface;
        }
        catch
        {
            // Fall back to CPU readback if sharing fails
            if (_d3d9Texture is not null)
            {
                _d3d9Texture->Release();
                _d3d9Texture = null;
            }

            _d3d9Surface = null;
        }
    }

    private void CreatePipeline()
    {
        var device = _device.Handle;

        // Shaders (vs_4_0 / ps_4_0: compatible with FL10 GPUs; SV_PrimitiveID is supported in SM 4.0)
        using var meshVsCode = CompileShader(HlslSource.Mesh, "VSMain", "vs_4_0");
        using var meshPsCode = CompileShader(HlslSource.Mesh, "PSMain", "ps_4_0");
        using var lineVsCode = CompileShader(HlslSource.Line, "VSMain", "vs_4_0");
        using var linePsCode = CompileShader(HlslSource.Line, "PSMain", "ps_4_0");
        using var pickVsCode = CompileShader(HlslSource.Pick, "VSMain", "vs_4_0");
        using var pickPsCode = CompileShader(HlslSource.Pick, "PSMain", "ps_4_0");
        using var pointVsCode = CompileShader(HlslSource.Point, "VSMain", "vs_4_0");
        using var pointPsCode = CompileShader(HlslSource.Point, "PSMain", "ps_4_0");
        using var glyphVsCode = CompileShader(HlslSource.Glyph, "VSMain", "vs_4_0");
        using var glyphPsCode = CompileShader(HlslSource.Glyph, "PSMain", "ps_4_0");

        SilkMarshal.ThrowHResult(device->CreateVertexShader(
            meshVsCode.GetBufferPointer(), meshVsCode.GetBufferSize(), null, _meshVs.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreatePixelShader(
            meshPsCode.GetBufferPointer(), meshPsCode.GetBufferSize(), null, _meshPs.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreateVertexShader(
            lineVsCode.GetBufferPointer(), lineVsCode.GetBufferSize(), null, _lineVs.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreatePixelShader(
            linePsCode.GetBufferPointer(), linePsCode.GetBufferSize(), null, _linePs.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreateVertexShader(
            pickVsCode.GetBufferPointer(), pickVsCode.GetBufferSize(), null, _pickVs.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreatePixelShader(
            pickPsCode.GetBufferPointer(), pickPsCode.GetBufferSize(), null, _pickPs.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreateVertexShader(
            pointVsCode.GetBufferPointer(), pointVsCode.GetBufferSize(), null, _pointVs.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreatePixelShader(
            pointPsCode.GetBufferPointer(), pointPsCode.GetBufferSize(), null, _pointPs.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreateVertexShader(
            glyphVsCode.GetBufferPointer(), glyphVsCode.GetBufferSize(), null, _glyphVs.GetAddressOf()));
        SilkMarshal.ThrowHResult(device->CreatePixelShader(
            glyphPsCode.GetBufferPointer(), glyphPsCode.GetBufferSize(), null, _glyphPs.GetAddressOf()));

        // Input layouts
        var semanticPosition = SilkMarshal.StringToMemory("POSITION");
        var semanticNormal = SilkMarshal.StringToMemory("NORMAL");
        var semanticTexCoord = SilkMarshal.StringToMemory("TEXCOORD");
        var semanticColor = SilkMarshal.StringToMemory("COLOR");
        try
        {
            fixed (byte* pPosition = semanticPosition)
            fixed (byte* pNormal = semanticNormal)
            fixed (byte* pTexCoord = semanticTexCoord)
            fixed (byte* pColor = semanticColor)
            {
                // Displacement (TEXCOORD1) is read from a separate buffer in slot 1
                var displacementElement = new InputElementDesc
                {
                    SemanticName = pTexCoord,
                    SemanticIndex = 1,
                    Format = DxgiFormat.FormatR32G32B32Float,
                    InputSlot = 1,
                    AlignedByteOffset = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                };

                // 20-byte vertex: position (12 B) + octahedral normal uint (4 B) + scalar (4 B)
                var meshElements = stackalloc InputElementDesc[4]
                {
                    new InputElementDesc
                    {
                        SemanticName = pPosition,
                        Format = DxgiFormat.FormatR32G32B32Float,
                        AlignedByteOffset = 0,
                        InputSlotClass = InputClassification.PerVertexData,
                    },
                    new InputElementDesc
                    {
                        SemanticName = pNormal,
                        Format = DxgiFormat.FormatR32Uint,
                        AlignedByteOffset = 12,
                        InputSlotClass = InputClassification.PerVertexData,
                    },
                    new InputElementDesc
                    {
                        SemanticName = pTexCoord,
                        Format = DxgiFormat.FormatR32Float,
                        AlignedByteOffset = 16,
                        InputSlotClass = InputClassification.PerVertexData,
                    },
                    displacementElement,
                };
                SilkMarshal.ThrowHResult(device->CreateInputLayout(
                    meshElements, 4,
                    meshVsCode.GetBufferPointer(), meshVsCode.GetBufferSize(),
                    _meshLayout.GetAddressOf()));

                var lineElements = stackalloc InputElementDesc[2]
                {
                    new InputElementDesc
                    {
                        SemanticName = pPosition,
                        Format = DxgiFormat.FormatR32G32B32Float,
                        AlignedByteOffset = 0,
                        InputSlotClass = InputClassification.PerVertexData,
                    },
                    displacementElement,
                };
                SilkMarshal.ThrowHResult(device->CreateInputLayout(
                    lineElements, 2,
                    lineVsCode.GetBufferPointer(), lineVsCode.GetBufferSize(),
                    _lineLayout.GetAddressOf()));

                // Pick pass: reads POSITION + displacement from the mesh vertex buffers (20 B stride)
                SilkMarshal.ThrowHResult(device->CreateInputLayout(
                    lineElements, 2,
                    pickVsCode.GetBufferPointer(), pickVsCode.GetBufferSize(),
                    _pickLayout.GetAddressOf()));

                // Point pass: position + corner + displacement (32 B stride, slot 0 only)
                var pointElements = stackalloc InputElementDesc[3]
                {
                    new InputElementDesc
                    {
                        SemanticName = pPosition,
                        Format = DxgiFormat.FormatR32G32B32Float,
                        AlignedByteOffset = 0,
                        InputSlotClass = InputClassification.PerVertexData,
                    },
                    new InputElementDesc
                    {
                        SemanticName = pTexCoord,
                        Format = DxgiFormat.FormatR32G32Float,
                        AlignedByteOffset = 12,
                        InputSlotClass = InputClassification.PerVertexData,
                    },
                    new InputElementDesc
                    {
                        SemanticName = pTexCoord,
                        SemanticIndex = 1,
                        Format = DxgiFormat.FormatR32G32B32Float,
                        AlignedByteOffset = 20,
                        InputSlotClass = InputClassification.PerVertexData,
                    },
                };
                SilkMarshal.ThrowHResult(device->CreateInputLayout(
                    pointElements, 3,
                    pointVsCode.GetBufferPointer(), pointVsCode.GetBufferSize(),
                    _pointLayout.GetAddressOf()));

                // Glyph pass: slot 0 = prototype vertex (24 B), slot 1 = per-instance data (56 B, step 1)
                var glyphElements = stackalloc InputElementDesc[7]
                {
                    new InputElementDesc
                    {
                        SemanticName = pPosition,
                        Format = DxgiFormat.FormatR32G32B32Float,
                        AlignedByteOffset = 0,
                        InputSlotClass = InputClassification.PerVertexData,
                    },
                    new InputElementDesc
                    {
                        SemanticName = pNormal,
                        Format = DxgiFormat.FormatR32G32B32Float,
                        AlignedByteOffset = 12,
                        InputSlotClass = InputClassification.PerVertexData,
                    },
                    new InputElementDesc
                    {
                        SemanticName = pTexCoord,
                        SemanticIndex = 4,
                        Format = DxgiFormat.FormatR32G32B32Float,
                        InputSlot = 1,
                        AlignedByteOffset = 0,
                        InputSlotClass = InputClassification.PerInstanceData,
                        InstanceDataStepRate = 1,
                    },
                    new InputElementDesc
                    {
                        SemanticName = pTexCoord,
                        SemanticIndex = 5,
                        Format = DxgiFormat.FormatR32G32B32Float,
                        InputSlot = 1,
                        AlignedByteOffset = 12,
                        InputSlotClass = InputClassification.PerInstanceData,
                        InstanceDataStepRate = 1,
                    },
                    new InputElementDesc
                    {
                        SemanticName = pTexCoord,
                        SemanticIndex = 6,
                        Format = DxgiFormat.FormatR32G32B32Float,
                        InputSlot = 1,
                        AlignedByteOffset = 24,
                        InputSlotClass = InputClassification.PerInstanceData,
                        InstanceDataStepRate = 1,
                    },
                    new InputElementDesc
                    {
                        SemanticName = pTexCoord,
                        SemanticIndex = 7,
                        Format = DxgiFormat.FormatR32Float,
                        InputSlot = 1,
                        AlignedByteOffset = 36,
                        InputSlotClass = InputClassification.PerInstanceData,
                        InstanceDataStepRate = 1,
                    },
                    new InputElementDesc
                    {
                        SemanticName = pColor,
                        Format = DxgiFormat.FormatR32G32B32A32Float,
                        InputSlot = 1,
                        AlignedByteOffset = 40,
                        InputSlotClass = InputClassification.PerInstanceData,
                        InstanceDataStepRate = 1,
                    },
                };
                SilkMarshal.ThrowHResult(device->CreateInputLayout(
                    glyphElements, 7,
                    glyphVsCode.GetBufferPointer(), glyphVsCode.GetBufferSize(),
                    _glyphLayout.GetAddressOf()));
            }
        }
        finally
        {
            semanticPosition.Dispose();
            semanticNormal.Dispose();
            semanticTexCoord.Dispose();
            semanticColor.Dispose();
        }

        // Unit-arrow prototype (+Z direction, total length 1): immutable buffers shared across all instances
        var (arrowVertices, arrowIndices) = ViewportGlyph.BuildArrowGeometry();
        _glyphIndexCount = (uint)arrowIndices.Length;
        fixed (float* pArrowVertices = arrowVertices)
        {
            _glyphVertexBuffer = CreateImmutableBufferRaw(
                device, pArrowVertices, (uint)(arrowVertices.Length * sizeof(float)), BindFlag.VertexBuffer);
        }

        fixed (int* pArrowIndices = arrowIndices)
        {
            _glyphIndexBuffer = CreateImmutableBufferRaw(
                device, pArrowIndices, (uint)(arrowIndices.Length * sizeof(int)), BindFlag.IndexBuffer);
        }

        // Constant buffer (dynamic, updated via Map/WriteDiscard per draw)
        var cbDesc = new BufferDesc
        {
            ByteWidth = (uint)sizeof(FrameConstants),
            Usage = Usage.Dynamic,
            BindFlags = (uint)BindFlag.ConstantBuffer,
            CPUAccessFlags = (uint)CpuAccessFlag.Write,
        };
        SilkMarshal.ThrowHResult(device->CreateBuffer(&cbDesc, null, _constantBuffer.GetAddressOf()));

        // Shared zero-displacement buffer (12 B) bound at slot 1 with stride 0 for undeformed meshes
        var zeroDisplacement = stackalloc float[3] { 0.0f, 0.0f, 0.0f };
        _zeroDisplacementBuffer = CreateImmutableBufferRaw(
            device, zeroDisplacement, 3 * sizeof(float), BindFlag.VertexBuffer);

        // Rasterizer: no culling so back sides of CAE shells remain visible
        var rsDesc = new RasterizerDesc
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.None,
            DepthClipEnable = 1,
            MultisampleEnable = 1,
        };
        SilkMarshal.ThrowHResult(device->CreateRasterizerState(&rsDesc, _rasterizerState.GetAddressOf()));

        // Depth state: LessEqual to support wireframe edge overlay
        var dsDesc = new DepthStencilDesc
        {
            DepthEnable = 1,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc = ComparisonFunc.LessEqual,
        };
        SilkMarshal.ThrowHResult(device->CreateDepthStencilState(&dsDesc, _depthState.GetAddressOf()));

        // Highlight depth state: depth test enabled without depth writes so translucent overlays do not corrupt depth
        var highlightDsDesc = dsDesc with { DepthWriteMask = DepthWriteMask.Zero };
        SilkMarshal.ThrowHResult(device->CreateDepthStencilState(
            &highlightDsDesc, _highlightDepthState.GetAddressOf()));

        // Alpha blending for semi-transparent parts and overlays
        var blendDesc = new BlendDesc();
        blendDesc.RenderTarget[0] = new RenderTargetBlendDesc
        {
            BlendEnable = 1,
            SrcBlend = Blend.SrcAlpha,
            DestBlend = Blend.InvSrcAlpha,
            BlendOp = BlendOp.Add,
            SrcBlendAlpha = Blend.One,
            DestBlendAlpha = Blend.InvSrcAlpha,
            BlendOpAlpha = BlendOp.Add,
            RenderTargetWriteMask = (byte)ColorWriteEnable.All,
        };
        SilkMarshal.ThrowHResult(device->CreateBlendState(&blendDesc, _alphaBlendState.GetAddressOf()));

        // Color-map sampler: point sampling to keep discrete contour bands crisp
        var samplerDesc = new SamplerDesc
        {
            Filter = Filter.MinMagMipPoint,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
        };
        SilkMarshal.ThrowHResult(device->CreateSamplerState(&samplerDesc, _colorMapSampler.GetAddressOf()));

        // Event query for WaitForGpuCompletion
        var queryDesc = new QueryDesc { Query = Query.Event };
        SilkMarshal.ThrowHResult(device->CreateQuery(&queryDesc, _frameCompleteQuery.GetAddressOf()));
    }

    private ComPtr<ID3D10Blob> CompileShader(string source, string entryPoint, string profile)
    {
        var bytes = Encoding.ASCII.GetBytes(source);

        ComPtr<ID3D10Blob> code = default;
        ComPtr<ID3D10Blob> errors = default;
        HResult hr = _compiler.Compile(
            in bytes[0],
            (nuint)bytes.Length,
            "WorkbenchViewport",
            null,
            ref Unsafe.NullRef<ID3DInclude>(),
            entryPoint,
            profile,
            0,
            0,
            ref code,
            ref errors);

        if (hr.IsFailure)
        {
            var message = errors.Handle is not null
                ? SilkMarshal.PtrToString((nint)errors.GetBufferPointer())
                : $"HRESULT 0x{hr.Value:X8}";
            errors.Dispose();
            throw new InvalidOperationException($"Failed to compile shader ({entryPoint}): {message}");
        }

        errors.Dispose();
        return code;
    }

    // ================= Render helpers =================

    private static ComPtr<ID3D11Buffer> CreateImmutableBufferRaw(
        ID3D11Device* device, void* data, uint byteWidth, BindFlag bindFlag)
    {
        var desc = new BufferDesc
        {
            ByteWidth = byteWidth,
            Usage = Usage.Immutable,
            BindFlags = (uint)bindFlag,
        };
        var subresource = new SubresourceData { PSysMem = data };

        ComPtr<ID3D11Buffer> buffer = default;
        SilkMarshal.ThrowHResult(device->CreateBuffer(&desc, &subresource, buffer.GetAddressOf()));
        return buffer;
    }

    private void UploadConstants(in FrameConstants constants)
    {
        var ctx = _context.Handle;
        var mapped = default(MappedSubresource);
        SilkMarshal.ThrowHResult(ctx->Map(
            (ID3D11Resource*)_constantBuffer.Handle, 0, Map.WriteDiscard, 0, &mapped));
        *(FrameConstants*)mapped.PData = constants;
        ctx->Unmap((ID3D11Resource*)_constantBuffer.Handle, 0);
    }

    private void BindVertexBuffer(ID3D11DeviceContext* ctx, GpuMeshChunk chunk)
    {
        // For chunks without displacements, bind the shared zero buffer at stride 0 (all vertices read (0,0,0))
        var hasDisplacement = chunk.DisplacementBufferHandle is not null;
        var buffers = stackalloc ID3D11Buffer*[2]
        {
            chunk.VertexBufferHandle,
            hasDisplacement ? chunk.DisplacementBufferHandle : _zeroDisplacementBuffer.Handle,
        };
        var strides = stackalloc uint[2]
        {
            GpuMesh.VertexStride,
            hasDisplacement ? GpuMesh.DisplacementStride : 0u,
        };
        var offsets = stackalloc uint[2] { 0, 0 };
        ctx->IASetVertexBuffers(0, 2, buffers, strides, offsets);
    }

    /// <summary>
    /// Renders the section plane indicator. Vertices are interleaved position + zero displacement (24 B);
    /// the same buffer is double-bound to slot 0 (offset 0) and slot 1 (offset 12) with stride 24 to match
    /// the 2-slot line shader layout without needing a dedicated shader.
    /// First 6 vertices are the quad triangle list, followed by 8 vertices for the outline line list.
    /// </summary>
    private void RenderSectionIndicator(
        ID3D11DeviceContext* ctx, float[] vertices, Vector4 fillColor, Vector4 lineColor,
        ref FrameConstants constants)
    {
        if (_sectionIndicatorBuffer.Handle is null)
        {
            var desc = new BufferDesc
            {
                ByteWidth = SectionIndicatorFloatCount * sizeof(float),
                Usage = Usage.Dynamic,
                BindFlags = (uint)BindFlag.VertexBuffer,
                CPUAccessFlags = (uint)CpuAccessFlag.Write,
            };
            SilkMarshal.ThrowHResult(_device.Handle->CreateBuffer(&desc, null, _sectionIndicatorBuffer.GetAddressOf()));
        }

        var mapped = default(MappedSubresource);
        SilkMarshal.ThrowHResult(ctx->Map(
            (ID3D11Resource*)_sectionIndicatorBuffer.Handle, 0, Map.WriteDiscard, 0, &mapped));
        fixed (float* pVertices = vertices)
        {
            Buffer.MemoryCopy(pVertices, mapped.PData,
                SectionIndicatorFloatCount * sizeof(float), SectionIndicatorFloatCount * sizeof(float));
        }

        ctx->Unmap((ID3D11Resource*)_sectionIndicatorBuffer.Handle, 0);

        var buffers = stackalloc ID3D11Buffer*[2] { _sectionIndicatorBuffer.Handle, _sectionIndicatorBuffer.Handle };
        var strides = stackalloc uint[2] { 24, 24 };
        var offsets = stackalloc uint[2] { 0, 12 };
        ctx->IASetVertexBuffers(0, 2, buffers, strides, offsets);
        ctx->IASetInputLayout(_lineLayout.Handle);
        ctx->VSSetShader(_lineVs.Handle, null, 0);
        ctx->PSSetShader(_linePs.Handle, null, 0);

        constants.ScalarParams.Z = 0.0f;
        constants.DeformParams.X = 0.0f;
        constants.ClipPlane = ViewportSection.DisabledClip;

        constants.ObjectColor = fillColor;
        UploadConstants(in constants);
        ctx->IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyTrianglelist);
        ctx->Draw(6, 0);

        constants.ObjectColor = lineColor;
        UploadConstants(in constants);
        ctx->IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyLinelist);
        ctx->Draw(8, 6);
    }

    // ================= Disposal =================

    private void ReleaseSizedResources()
    {
        if (_d3d9Surface is not null)
        {
            _d3d9Surface->Release();
            _d3d9Surface = null;
        }

        if (_d3d9Texture is not null)
        {
            _d3d9Texture->Release();
            _d3d9Texture = null;
        }

        _stagingTexture.Dispose();
        _resolveTexture.Dispose();
        _depthView.Dispose();
        _depthTexture.Dispose();
        _msaaRtv.Dispose();
        _msaaColor.Dispose();
        _pickDepthView.Dispose();
        _pickDepthTexture.Dispose();
        _pickRtv.Dispose();
        _pickTexture.Dispose();
        _hoverStagingTexture.Dispose();
        _hoverStagingTexture = default;
        _stagingTexture = default;
        _resolveTexture = default;
        _depthView = default;
        _depthTexture = default;
        _msaaRtv = default;
        _msaaColor = default;
        _pickDepthView = default;
        _pickDepthTexture = default;
        _pickRtv = default;
        _pickTexture = default;
    }

    private void ReleaseD3D9()
    {
        if (_d3d9Device is not null)
        {
            _d3d9Device->Release();
            _d3d9Device = null;
        }

        if (_d3d9 is not null)
        {
            _d3d9->Release();
            _d3d9 = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ReleaseSizedResources();
        ReleaseD3D9();

        _colorMapSrv.Dispose();
        _colorMapTexture.Dispose();
        _colorMapSampler.Dispose();
        _alphaBlendState.Dispose();
        _highlightDepthState.Dispose();
        _depthState.Dispose();
        _rasterizerState.Dispose();
        _zeroDisplacementBuffer.Dispose();
        _frameCompleteQuery.Dispose();
        _glyphIndexBuffer.Dispose();
        _glyphVertexBuffer.Dispose();
        _sectionIndicatorBuffer.Dispose();
        _constantBuffer.Dispose();
        _glyphLayout.Dispose();
        _pointLayout.Dispose();
        _pickLayout.Dispose();
        _lineLayout.Dispose();
        _meshLayout.Dispose();
        _glyphPs.Dispose();
        _glyphVs.Dispose();
        _pointPs.Dispose();
        _pointVs.Dispose();
        _pickPs.Dispose();
        _pickVs.Dispose();
        _linePs.Dispose();
        _lineVs.Dispose();
        _meshPs.Dispose();
        _meshVs.Dispose();
        _context.Dispose();
        _device.Dispose();

        _colorMapSrv = default;
        _colorMapTexture = default;
        _colorMapSampler = default;
        _alphaBlendState = default;
        _highlightDepthState = default;
        _depthState = default;
        _rasterizerState = default;
        _zeroDisplacementBuffer = default;
        _frameCompleteQuery = default;
        _glyphIndexBuffer = default;
        _glyphVertexBuffer = default;
        _sectionIndicatorBuffer = default;
        _constantBuffer = default;
        _glyphLayout = default;
        _pointLayout = default;
        _pickLayout = default;
        _lineLayout = default;
        _meshLayout = default;
        _glyphPs = default;
        _glyphVs = default;
        _pointPs = default;
        _pointVs = default;
        _pickPs = default;
        _pickVs = default;
        _linePs = default;
        _lineVs = default;
        _meshPs = default;
        _meshVs = default;
        _context = default;
        _device = default;

        _d3d9Api?.Dispose();
        _d3d9Api = null;
        _compiler.Dispose();
        _d3d11.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern nint GetDesktopWindow();
}
