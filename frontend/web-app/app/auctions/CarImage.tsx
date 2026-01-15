"use client";

import { useState } from 'react';
import  Image  from 'next/image';

type Props = {
    imageUrl: string;
}

export default function CarImage({ imageUrl }: Props) {
    const [loading, setLoading] = useState(true);
    
    // Provera da li je validan URL
    const isValidUrl = imageUrl && (imageUrl.startsWith('http://') || imageUrl.startsWith('https://') || imageUrl.startsWith('/'));
    
    if (!isValidUrl) {
        return <div className="flex items-center justify-center h-full bg-gray-200 text-gray-500">No image</div>;
    }
    
    return (
        <Image
            src={imageUrl}
            alt='Image of car'
            fill
            className={
                `
                object-cover duration-700 ease-in-out
                ${loading ? 'opacity-0 scale-110' : 'opacity-100 scale-100'}
                `
            }
            priority
            sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 25vw"
            onLoad={() => setLoading(false)}
            unoptimized
        />
    )
}
