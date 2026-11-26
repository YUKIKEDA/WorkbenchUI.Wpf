import type {ReactNode} from 'react';
import Link from '@docusaurus/Link';
import Translate, {translate} from '@docusaurus/Translate';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';

import styles from './index.module.css';

export default function Home(): ReactNode {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={siteConfig.title}
      description={translate({
        id: 'homepage.description',
        message: 'High-density, low-chroma WPF UI library for engineering & scientific desktop workbenches on .NET 10.',
      })}>
      <header className={styles.heroBanner}>
        <div className="container">
          <Heading as="h1" className="hero__title">
            {siteConfig.title}
          </Heading>
          <p className="hero__subtitle">
            <Translate id="homepage.tagline">
              High-density, low-chroma WPF UI library for engineering &amp; scientific desktop workbenches on .NET 10.
            </Translate>
          </p>
          <div className={styles.buttons}>
            <Link className="button button--primary button--lg" to="/docs/tutorials/first-workbench-app">
              <Translate id="homepage.cta.tutorial">Get Started Tutorial</Translate>
            </Link>
            <Link className="button button--secondary button--lg" to="/docs/intro">
              <Translate id="homepage.cta.docs">Read the Docs</Translate>
            </Link>
          </div>
        </div>
      </header>

      <main>
        <section className={styles.section}>
          <div className="container">
            <Heading as="h2">
              <Translate id="homepage.diataxis.title">Documentation Guide (Diátaxis)</Translate>
            </Heading>
            <div className={styles.grid}>
              <Link className={styles.card} to="/docs/tutorials/first-workbench-app">
                <h3><Translate id="homepage.q.tutorials.title">Tutorials</Translate></h3>
                <p><Translate id="homepage.q.tutorials.desc">Step-by-step hands-on guide to building your first engineering workbench window.</Translate></p>
              </Link>
              <Link className={styles.card} to="/docs/how-to/theming-and-accent">
                <h3><Translate id="homepage.q.howto.title">How-to Guides</Translate></h3>
                <p><Translate id="homepage.q.howto.desc">Task-oriented recipes for theming, unit providers, PropertyGrid, ModelTree, charts, docking, and 3D viewports.</Translate></p>
              </Link>
              <Link className={styles.card} to="/docs/reference/controls-catalog">
                <h3><Translate id="homepage.q.ref.title">Reference</Translate></h3>
                <p><Translate id="homepage.q.ref.desc">Complete catalogs of design tokens, controls, built-in icons, and extension package APIs.</Translate></p>
              </Link>
              <Link className={styles.card} to="/docs/explanation/theme-architecture">
                <h3><Translate id="homepage.q.exp.title">Explanation</Translate></h3>
                <p><Translate id="homepage.q.exp.desc">Architectural rationale behind the neutral-gray theme system, zero-dependency core, and design decisions.</Translate></p>
              </Link>
            </div>
          </div>
        </section>

        <section className={styles.sectionAlt}>
          <div className="container">
            <Heading as="h2">
              <Translate id="homepage.packages.title">Modular Packages</Translate>
            </Heading>
            <div className={styles.grid}>
              <Link className={styles.card} to="/docs/reference/controls-catalog">
                <h3>WorkbenchUI.Wpf</h3>
                <p><Translate id="homepage.pkg.core">Zero-dependency core: semantic Dark/Light theme, 68 vector icons, and high-density workbench controls.</Translate></p>
              </Link>
              <Link className={styles.card} to="/docs/reference/extension-packages#workbenchuiwpfcharts">
                <h3>WorkbenchUI.Wpf.Charts</h3>
                <p><Translate id="homepage.pkg.charts">ScottPlot 5 theme synchronization and engineering plots (convergence, history, Bode FRF, histogram).</Translate></p>
              </Link>
              <Link className={styles.card} to="/docs/reference/extension-packages#workbenchuiwpfdocking">
                <h3>WorkbenchUI.Wpf.Docking</h3>
                <p><Translate id="homepage.pkg.docking">AvalonDock theme bound dynamically to Wb.Color.* tokens plus ContentId layout serialization helpers.</Translate></p>
              </Link>
              <Link className={styles.card} to="/docs/reference/extension-packages#workbenchuiwpfviewport3d">
                <h3>WorkbenchUI.Wpf.Viewport3D</h3>
                <p><Translate id="homepage.pkg.viewport">Custom Direct3D 11 viewport for surface meshes, sparse voxels, contours, deformation, sectioning, and probing.</Translate></p>
              </Link>
            </div>
          </div>
        </section>
      </main>
    </Layout>
  );
}
