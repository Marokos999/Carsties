import { Auction, PagedResult } from "@/types";
import { create } from "zustand";

type State = {
    auction: Auction[];
    totalCount: number;
    pageCount: number;
}

type Actions = {
    setData: (data: PagedResult<Auction>) => void;
    setCurrentPrice: (auctionId: string, amount: number) => void;
}

const initialState : State = {
    auction: [],
    totalCount: 0,
    pageCount: 0    
}

export const useAuctionState = create<State & Actions>((set) => ({ 
    ...initialState,
    setData: (data: PagedResult<Auction>) => set(() => ({
        auction: data.results,
        totalCount: data.totalCount,
        pageCount: data.pageCount
    })),
    setCurrentPrice: (auctionId: string, amount: number) => set((state) => ({
        auction: state.auction.map((auction) =>
            auction.id === auctionId ? { ...auction, currentPrice: amount } : auction
        )
    }))
}));