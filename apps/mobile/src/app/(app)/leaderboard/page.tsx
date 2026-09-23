import { Trophy } from 'lucide-react';

import { ComingSoon } from '@/components/atoms/ComingSoon';

const TITLE = 'Leaderboard';
const DESCRIPTION = 'See how you stack up against players worldwide.';

export const metadata = {
    title: 'Leaderboard · Xpeak',
};

export default function LeaderboardPage() {
    return <ComingSoon title={TITLE} Icon={Trophy} description={DESCRIPTION} />;
}
