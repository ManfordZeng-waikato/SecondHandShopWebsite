import { HomeHeroSection } from '../features/home/components/HomeHeroSection';
import { FeaturedProductsSection } from '../features/home/components/FeaturedProductsSection';
import { OurStorySection } from '../features/home/components/OurStorySection';

export function HomePage() {
  return (
    <>
      <HomeHeroSection />
      <FeaturedProductsSection />
      <OurStorySection />
    </>
  );
}
