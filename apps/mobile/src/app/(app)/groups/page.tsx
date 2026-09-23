import { Users } from 'lucide-react';

import { ComingSoon } from '@/components/atoms/ComingSoon';

const TITLE = 'Groups';
const DESCRIPTION = 'Train together. Push each other. Level up as a crew.';

export const metadata = {
    title: 'Groups · Xpeak',
};

export default function GroupsPage() {
    return <ComingSoon title={TITLE} Icon={Users} description={DESCRIPTION} />;
}
