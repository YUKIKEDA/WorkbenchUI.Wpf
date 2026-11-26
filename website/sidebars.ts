import type { SidebarsConfig } from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  docsSidebar: [
    'intro',
    {
      type: 'category',
      label: 'Tutorials',
      items: ['tutorials/first-workbench-app'],
    },
    {
      type: 'category',
      label: 'How-to Guides',
      items: [
        'how-to/theming-and-accent',
        'how-to/units-and-numeric-input',
        'how-to/property-grid',
        'how-to/model-tree',
        'how-to/docking-and-charts',
        'how-to/viewport-3d',
      ],
    },
    {
      type: 'category',
      label: 'Reference',
      items: [
        'reference/design-tokens',
        'reference/controls-catalog',
        'reference/icons',
        'reference/extension-packages',
      ],
    },
    {
      type: 'category',
      label: 'Explanation',
      items: [
        'explanation/theme-architecture',
        'explanation/package-architecture',
        'design-decisions',
      ],
    },
  ],
};

export default sidebars;
