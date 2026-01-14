'use client'

import { useState } from 'react';
import { Pagination } from "flowbite-react";

type Props = {
  currentPage: number;
  pageCount: number;
}

export default function AppPagination({ currentPage, pageCount }: Props) {
  const [pageNumber, setPageNumber] = useState(currentPage);
    return (
     <Pagination
      currentPage={pageNumber}
      onPageChange={e =>  setPageNumber(e)}
      totalPages={pageCount}
      layout='pagination'
      showIcons={true}
      className='text-blue-500 mb-5'
     />
  )
}
