import DogCard from './DogCard';
import type { DogDto, AdoptedDogDto } from '../types/api';

interface DogGridProps {
  dogs: DogDto[] | AdoptedDogDto[];
  adopted?: boolean;
}

export default function DogGrid({ dogs, adopted = false }: DogGridProps) {
  return (
    <div className="dog-grid">
      {dogs.map((dog, index) => (
        <DogCard key={dog.aid} dog={dog} index={index} adopted={adopted} />
      ))}
    </div>
  );
}
