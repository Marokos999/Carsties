'use client'

import AuctionCard from './AuctionCard';
import AppPagination from '../components/AppPagination';
import { getData } from '../actions/actionAuctions';
import { useEffect, useState } from 'react';
import { Auction, PagedResult } from '@/types';
import Filters from './Filters';
import { useShallow } from 'zustand/shallow';
import { useParamsStore } from '@/hooks/useParamsStore';
import qs from 'query-string';
import EmptyFilter from '../components/EmptyFilter';

export default function Listings() {
    const [data, setData] = useState<PagedResult<Auction>>();
    const params = useParamsStore(
        useShallow((state) => ({
            pageNumber: state.pageNumber,
            pageSize: state.pageSize,
            searchTerm: state.searchTerm,
            orderBy: state.orderBy,
            filterBy: state.filterBy,
        }))
    );

    const setParams = useParamsStore((state) => state.setParams);

    const query = qs.stringify(
        {
            pageNumber: params.pageNumber,
            pageSize: params.pageSize,
            searchTerm: params.searchTerm,
            orderBy: params.orderBy,
            filterBy: params.filterBy
        },
        { skipEmptyString: true, skipNull: true }
    );

    function setPageNumber(pageNumber: number) {
        setParams({ pageNumber });
    }

    useEffect(() => {
        getData(query).then((res) => setData(res));
    }, [query]);

    if (!data) return <h3>Loading...</h3>;

    return (
        <>
            <Filters />
            {data.totalCount === 0 ? (
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

