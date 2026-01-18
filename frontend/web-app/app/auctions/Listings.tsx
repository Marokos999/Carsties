'use client'

import AuctionCard from './AuctionCard';
import AppPagination from '../components/AppPagination';
import { getData } from '../actions/auctionActions';
import { useEffect, useState } from 'react';
import Filters from './Filters';
import { useShallow } from 'zustand/shallow';
import { useParamsStore } from '@/hooks/useParamsStore';
import qs from 'query-string';
import EmptyFilter from '../components/EmptyFilter';
import { useAuctionState } from '@/hooks/useAuctionState';

export default function Listings() {
    const [loading, setLoading] = useState(true);
    const params = useParamsStore(
        useShallow((state) => ({
            pageNumber: state.pageNumber,
            pageSize: state.pageSize,
            searchTerm: state.searchTerm,
            orderBy: state.orderBy,
            filterBy: state.filterBy,
            seller: state.seller,
            winner: state.winner
        }))
    );

    const data = useAuctionState(useShallow((state) => ({
        results: state.auction,
        totalCount: state.totalCount,
        pageCount: state.pageCount
    })));
    const setData = useAuctionState((state) => state.setData);
    const setParams = useParamsStore((state) => state.setParams);

    const url = qs.stringify(
        {
            pageNumber: params.pageNumber,
            pageSize: params.pageSize,
            searchTerm: params.searchTerm,
            orderBy: params.orderBy,
            filterBy: params.filterBy,
            seller: params.seller,
            winner: params.winner
        },
        { skipEmptyString: true, skipNull: true }
    );

    function setPageNumber(pageNumber: number) {
        setParams({ pageNumber });
    }

    useEffect(() => {
        getData(url).then((res) => {
        setData(res);
        setLoading(false);
        });
    }, [url, setData]);
    
    if (loading) return <h3>Loading...</h3>;

    return (
        <>
            <Filters />
            {!data.results || data.totalCount === 0 ? (
             <EmptyFilter  showReset={true} />
            ) : (
                <>
                 <div className="grid grid-cols-4 gap-6">
                {data.results.map((auction) => (
                    <AuctionCard key={auction.id} auction={auction} />
                ))}
            </div>
            <div className="flex justify-center mt-4">
                <AppPagination
                    currentPage={params.pageNumber}
                    pageCount={data.pageCount}
                    pageChanged={setPageNumber}
                />
            </div>
                </>
            )}
           
        </>
    );
}

