'use client'

import { useAuctionState } from '@/hooks/useAuctionState';
import { useBidStore } from '@/hooks/useBidStore';
import { Auction, AuctionFinished, Bid } from '@/types';
import { HubConnection } from '@microsoft/signalr/dist/esm/HubConnection';
import { User } from 'next-auth';
import { useParams } from 'next/dist/client/components/navigation';
import React, { ReactNode, useCallback, useEffect, useRef } from 'react'
import toast from 'react-hot-toast';
import AuctionCreatedToast from '../components/AuctionCreatedToast';
import AuctionFinishedToast from '../components/AuctionFinishedToast';
import { HubConnectionBuilder } from '@microsoft/signalr/dist/esm/HubConnectionBuilder';
import { getDetailedViewData } from '../actions/auctionActions';

type Props = {
    children: ReactNode
    user: User | null
}

export default function SignalRProvider({ children, user }: Props) {
    const connection = useRef<HubConnection | null>(null);
    const setCurrentPrice = useAuctionState(state => state.setCurrentPrice);
    const addBid = useBidStore(state => state.addBid);
    const params = useParams<{ id: string }>();

        const handleAuctionCreated = useCallback((auction: Auction) => {
        if (user?.username !== auction.seller) {
            return toast(<AuctionCreatedToast auction={auction} />, {
                duration: 10000,
            })
        }
        }, [user])

        const handleBidPlaced = useCallback((bid: Bid) => {
        if (bid.bidStatus.includes('Accepted')) {
            setCurrentPrice(bid.auctionId, bid.amount);
        }

        if (params.id === bid.auctionId) {
            addBid(bid);
        }
        }, [setCurrentPrice, addBid, params.id]);

        const handleAuctionFinished = useCallback((finishedAuction: AuctionFinished) => {
            setCurrentPrice(finishedAuction.auctionId, finishedAuction.amount || 0);
            
            getDetailedViewData(finishedAuction.auctionId).then(auction => {
                toast(<AuctionFinishedToast 
                    auction={auction} 
                    finishedAuction={finishedAuction} 
                />, { duration: 10000 });
            });
        }, [setCurrentPrice]);

        useEffect(() => {
        if (!connection.current) {
            connection.current = new HubConnectionBuilder()
                .withUrl(process.env.NEXT_PUBLIC_NOTIFICATION_URL!)
                .withAutomaticReconnect()
                .build();

            connection.current.start()
                .then(() => console.log('Connected to notification hub'))
                .catch(err => console.error('Error connecting to SignalR hub:', err));
        }

            connection.current.on('BidPlaced', handleBidPlaced);
            connection.current.on('AuctionCreated', handleAuctionCreated);
            connection.current.on('AuctionFinished', handleAuctionFinished);

        return () => {
            connection.current?.off('BidPlaced', handleBidPlaced);
            connection.current?.off('AuctionCreated', handleAuctionCreated);
            connection.current?.off('AuctionFinished', handleAuctionFinished);
        }
        }, [handleBidPlaced, handleAuctionCreated, handleAuctionFinished]);

    
  return (
    <>{children}</>
  )
}
