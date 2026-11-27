import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'WorkbenchUI.Wpf',
  tagline: 'High-density, low-chroma WPF UI library for engineering & scientific desktop workbenches',
  favicon: 'img/logo.svg',

  url: 'https://yukikeda.github.io',
  baseUrl: '/WorkbenchUI.Wpf/',

  organizationName: 'YUKIKEDA',
  projectName: 'WorkbenchUI.Wpf',
  trailingSlash: false,

  onBrokenLinks: 'throw',
  onBrokenAnchors: 'throw',
  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'throw',
    },
  },

  i18n: {
    defaultLocale: 'en',
    locales: ['en', 'ja'],
    localeConfigs: {
      en: {
        label: 'English',
      },
      ja: {
        label: '日本語',
      },
    },
  },

  presets: [
    [
      'classic',
      {
        docs: {
          path: '../docs',
          routeBasePath: 'docs',
          sidebarPath: './sidebars.ts',
          exclude: ['design-decisions.md'],
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    colorMode: {
      defaultMode: 'dark',
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'WorkbenchUI.Wpf',
      logo: {
        alt: 'WorkbenchUI.Wpf Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          to: '/docs/tutorials/first-workbench-app',
          label: 'Tutorials',
          position: 'left',
        },
        {
          to: '/docs/how-to/theming-and-accent',
          label: 'How-to Guides',
          position: 'left',
        },
        {
          to: '/docs/reference/controls-catalog',
          label: 'Reference',
          position: 'left',
        },
        {
          to: '/docs/explanation/theme-architecture',
          label: 'Explanation',
          position: 'left',
        },
        {
          type: 'localeDropdown',
          position: 'right',
        },
        {
          href: 'https://github.com/YUKIKEDA/WorkbenchUI.Wpf',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Documentation',
          items: [
            {label: 'Introduction', to: '/docs/intro'},
            {label: 'Tutorials', to: '/docs/tutorials/first-workbench-app'},
            {label: 'How-to Guides', to: '/docs/how-to/theming-and-accent'},
            {label: 'Reference', to: '/docs/reference/controls-catalog'},
            {label: 'Explanation', to: '/docs/explanation/theme-architecture'},
          ],
        },
        {
          title: 'Packages',
          items: [
            {label: 'WorkbenchUI.Wpf', to: '/docs/reference/controls-catalog'},
            {label: 'WorkbenchUI.Wpf.Charts', to: '/docs/reference/extension-packages#workbenchuiwpfcharts'},
            {label: 'WorkbenchUI.Wpf.Docking', to: '/docs/reference/extension-packages#workbenchuiwpfdocking'},
            {label: 'WorkbenchUI.Wpf.Viewport3D', to: '/docs/reference/extension-packages#workbenchuiwpfviewport3d'},
          ],
        },
        {
          title: 'Project',
          items: [
            {label: 'GitHub', href: 'https://github.com/YUKIKEDA/WorkbenchUI.Wpf'},
            {label: 'MIT License', href: 'https://github.com/YUKIKEDA/WorkbenchUI.Wpf/blob/main/LICENSE'},
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} YUKIKEDA. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.vsLight,
      darkTheme: prismThemes.vsDark,
      additionalLanguages: ['csharp', 'powershell', 'bash'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
