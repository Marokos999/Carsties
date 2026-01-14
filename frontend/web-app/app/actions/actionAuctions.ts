// Client-friendly fetch helper (no server actions)

import { PagedResult, Auction } from "@/types";

export async function getData(query: string): Promise<PagedResult<Auction>> {
    const url = `http://localhost:6001/search${query ? `?${query}` : ''}`;
    const res = await fetch(url, { cache: 'no-store' });

    if (!res.ok) throw new Error('Failed to fetch data');

    return res.json();
}