import { Plus } from 'lucide-react';

import { ComingSoon } from '@/components/atoms/ComingSoon';

const TITLE = 'New check-in';
const DESCRIPTION = 'Log a workout with media proof and rack up XP.';

export const metadata = {
    title: 'Check-in · Xpeak',
};

export default function CheckinPage() {
    return <ComingSoon title={TITLE} Icon={Plus} description={DESCRIPTION} />;
}
