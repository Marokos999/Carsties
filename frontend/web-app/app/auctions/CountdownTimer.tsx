"use client";

import Countdown, { zeroPad } from "react-countdown";

const renderer = (props: any) => {
  const { days, hours, minutes, seconds, completed } = props;

  const statusClass = completed
    ? "bg-red-600"
    : days === 0 && hours < 10
    ? "border-r-amber-600"
    : "bg-green-600";

  return (
    <div className={`border-2 border-white text-white py-2 px-2 rounded-lg flex justify-center ${statusClass}`}>
      {completed ? "Finished" : `${days}d ${zeroPad(hours)}h ${zeroPad(minutes)}m ${zeroPad(seconds)}s`}
    </div>
  );
};

type Props = {
    auctionEnd : string;
}

export default function CountdownTimer({ auctionEnd }: Props) {
  return (
    <div>
        <Countdown date={new Date(auctionEnd)} renderer={renderer} />
    </div>
  )
}
