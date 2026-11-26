namespace WorkbenchUI.Wpf.Viewport3D.Rendering;

/// <summary>
/// Embedded HLSL shader sources compiled at runtime via d3dcompiler_47 (included with Windows).
/// Uses row_major matrices and mul(v, M) to match System.Numerics row-major, row-vector conventions
/// without transposing on upload.
/// </summary>
internal static class HlslSource
{
    /// <summary>
    /// Constant buffer layout shared across mesh, line, pick, glyph, and point shaders (must match <see cref="FrameConstants"/>).
    /// </summary>
    private const string Constants = """
        cbuffer FrameConstants : register(b0)
        {
            row_major float4x4 ViewProj;
            float4 EyeDirection;   // xyz = unit vector from target to eye (headlight direction)
            float4 ObjectColor;    // Solid part color / edge line color / pick pass: x = part ID
            float4 ScalarParams;   // x=min, y=1/range, z=contour enabled, w=log scale
            float4 NaNColor;
            float4 BelowColor;
            float4 AboveColor;
            float4 ViewportInfo;   // xy=viewport pixel size, z=point diameter (px)
            float4 DeformParams;   // x=deformation scale (including animation factor), y=glyph scale
            uint4  PickParams;     // x=chunk base global triangle offset for picking
            float4 ClipPlane;      // xyz=normalized local-space normal, w=constant; (0,0,0,1) when disabled
        };
        """;

    /// <summary>Triangle mesh shader (solid or contour, two-sided headlight shading).</summary>
    public const string Mesh = Constants + """

        Texture2D ColorMapTex : register(t0);
        SamplerState ColorMapSampler : register(s0);

        struct VSIn
        {
            float3 pos          : POSITION;
            uint   packedNormal : NORMAL;   // Octahedral 16-bit x 2 (20 B vertex stride)
            float  scalar       : TEXCOORD0;
            float3 disp         : TEXCOORD1;
        };

        struct PSIn
        {
            float4 pos    : SV_Position;
            float3 normal : NORMAL;
            float  scalar : TEXCOORD0;
            float  clip   : SV_ClipDistance0;
        };

        // Octahedral 16-bit x 2 -> unit normal (matches ViewportGeometry.DecodeOctahedralNormal)
        float3 DecodeOctNormal(uint p)
        {
            float2 e = float2(p & 0xFFFFu, p >> 16) / 65535.0 * 2.0 - 1.0;
            float3 v = float3(e.x, e.y, 1.0 - abs(e.x) - abs(e.y));
            if (v.z < 0.0)
            {
                float2 s = float2(e.x >= 0.0 ? 1.0 : -1.0, e.y >= 0.0 ? 1.0 : -1.0);
                v.xy = (1.0 - abs(e.yx)) * s;
            }
            return normalize(v);
        }

        PSIn VSMain(VSIn v)
        {
            PSIn o;
            // Apply deformation in the vertex shader; keep undeformed normals
            // (sufficient for small-strain CAE visualization and avoids shading flicker).
            float3 pos = v.pos + v.disp * DeformParams.x;
            o.pos = mul(float4(pos, 1.0), ViewProj);
            o.normal = DecodeOctNormal(v.packedNormal);
            o.scalar = v.scalar;
            // Section cut: evaluate signed distance at deformed position and clip negative side
            o.clip = dot(pos, ClipPlane.xyz) + ClipPlane.w;
            return o;
        }

        float4 PSMain(PSIn i) : SV_Target
        {
            float3 baseColor = ObjectColor.rgb;

            if (ScalarParams.z > 0.5)
            {
                float s = i.scalar;
                if (isnan(s))
                {
                    if (NaNColor.a < 0.01)
                    {
                        discard;
                    }
                    baseColor = NaNColor.rgb;
                }
                else
                {
                    if (ScalarParams.w > 0.5)
                    {
                        s = log10(max(s, 1e-30));
                    }
                    float u = (s - ScalarParams.x) * ScalarParams.y;
                    if (u < 0.0)
                    {
                        baseColor = BelowColor.rgb;
                    }
                    else if (u > 1.0)
                    {
                        baseColor = AboveColor.rgb;
                    }
                    else
                    {
                        baseColor = ColorMapTex.Sample(ColorMapSampler, float2(u, 0.5)).rgb;
                    }
                }
            }

            // Two-sided headlight: take abs so front and back shell faces shade symmetrically
            float3 n = normalize(i.normal);
            float ndl = abs(dot(n, EyeDirection.xyz));
            float3 lit = baseColor * (0.35 + 0.65 * ndl);
            return float4(lit, ObjectColor.a);
        }
        """;

    /// <summary>
    /// Line shader for wireframe edge overlay.
    /// Nudges NDC depth slightly forward to avoid Z-fighting; setting DeformParams.x = 0 renders undeformed wireframe.
    /// </summary>
    public const string Line = Constants + """

        struct VSIn
        {
            float3 pos  : POSITION;
            float3 disp : TEXCOORD1;
        };

        struct PSIn
        {
            float4 pos  : SV_Position;
            float  clip : SV_ClipDistance0;
        };

        PSIn VSMain(VSIn v)
        {
            PSIn o;
            float3 pos = v.pos + v.disp * DeformParams.x;
            o.pos = mul(float4(pos, 1.0), ViewProj);
            o.pos.z -= 0.0005 * o.pos.w;
            o.clip = dot(pos, ClipPlane.xyz) + ClipPlane.w;
            return o;
        }

        float4 PSMain(PSIn i) : SV_Target
        {
            return ObjectColor;
        }
        """;

    /// <summary>
    /// GPU ID picking pass. Writes R = part ID + 1 (0 = background) and G = global triangle index
    /// into an R32G32_UInt render target.
    /// </summary>
    public const string Pick = Constants + """

        struct VSIn
        {
            float3 pos  : POSITION;
            float3 disp : TEXCOORD1;
        };

        struct PSIn
        {
            float4 pos  : SV_Position;
            float  clip : SV_ClipDistance0;
        };

        PSIn VSMain(VSIn v)
        {
            PSIn o;
            // Apply the same deformation and section clipping as the display pass so picking matches what is visible
            float3 pos = v.pos + v.disp * DeformParams.x;
            o.pos = mul(float4(pos, 1.0), ViewProj);
            o.clip = dot(pos, ClipPlane.xyz) + ClipPlane.w;
            return o;
        }

        uint2 PSMain(PSIn i, uint primId : SV_PrimitiveID) : SV_Target
        {
            // SV_PrimitiveID resets per draw call, so add the chunk's global triangle base (PickParams.x)
            return uint2((uint)(ObjectColor.x + 0.5), primId + PickParams.x);
        }
        """;

    /// <summary>
    /// Vector glyph shader. Renders unit-arrow prototype (slot 0) via DrawIndexedInstanced with per-instance
    /// data (slot 1: base position, displacement, unit direction, magnitude, color). Actual length is |v| * DeformParams.y.
    /// Section clipping is evaluated at the anchor node so whole arrows disappear cleanly without partial slicing.
    /// </summary>
    public const string Glyph = Constants + """

        struct VSIn
        {
            // slot 0: prototype vertex (unit arrow along +Z)
            float3 pos       : POSITION;
            float3 normal    : NORMAL;
            // slot 1: per-instance attributes
            float3 base_     : TEXCOORD4;
            float3 disp      : TEXCOORD5;
            float3 dir       : TEXCOORD6;
            float  magnitude : TEXCOORD7;
            float4 color     : COLOR0;
        };

        struct PSIn
        {
            float4 pos    : SV_Position;
            float3 normal : NORMAL;
            float4 color  : COLOR0;
            float  clip   : SV_ClipDistance0;
        };

        PSIn VSMain(VSIn v)
        {
            // Build orthonormal basis from unit direction w (matches ViewportGlyph.ComputeBasis)
            float3 w = v.dir;
            float3 helper = abs(w.z) < 0.9 ? float3(0.0, 0.0, 1.0) : float3(1.0, 0.0, 0.0);
            float3 u = normalize(cross(helper, w));
            float3 vv = cross(w, u);

            float len = v.magnitude * DeformParams.y; // Physical arrow length (uniformly scales thickness too)
            float3 anchor = v.base_ + v.disp * DeformParams.x; // Follow deformation
            float3 pos = anchor + (v.pos.x * u + v.pos.y * vv + v.pos.z * w) * len;

            PSIn o;
            o.pos = mul(float4(pos, 1.0), ViewProj);
            o.normal = v.normal.x * u + v.normal.y * vv + v.normal.z * w; // Rotation only due to uniform scaling
            o.color = v.color;
            o.clip = dot(anchor, ClipPlane.xyz) + ClipPlane.w;
            return o;
        }

        float4 PSMain(PSIn i) : SV_Target
        {
            // Same two-sided headlight shading as mesh
            float3 n = normalize(i.normal);
            float ndl = abs(dot(n, EyeDirection.xyz));
            return float4(i.color.rgb * (0.35 + 0.65 * ndl), i.color.a);
        }
        """;

    /// <summary>
    /// Point shader for selected nodes. Expands 6-vertex quads (corner offsets in [-0.5, 0.5]^2)
    /// to pixel size in clip space and discards outside a circle.
    /// </summary>
    public const string Point = Constants + """

        struct VSIn
        {
            float3 pos    : POSITION;
            float2 corner : TEXCOORD0;
            float3 disp   : TEXCOORD1;
        };

        struct PSIn
        {
            float4 pos    : SV_Position;
            float2 corner : TEXCOORD0;
            float  clipDistance : SV_ClipDistance0;
        };

        PSIn VSMain(VSIn v)
        {
            PSIn o;
            float3 pos = v.pos + v.disp * DeformParams.x;
            o.clipDistance = dot(pos, ClipPlane.xyz) + ClipPlane.w;
            float4 clip = mul(float4(pos, 1.0), ViewProj);
            clip.z -= 0.001 * clip.w; // Nudge forward to ensure visibility over faces
            clip.xy += v.corner * ViewportInfo.z * (2.0 / ViewportInfo.xy) * clip.w;
            o.pos = clip;
            o.corner = v.corner;
            return o;
        }

        float4 PSMain(PSIn i) : SV_Target
        {
            if (dot(i.corner, i.corner) > 0.25)
            {
                discard; // Clip quad to circular point
            }
            return ObjectColor;
        }
        """;
}
