import { Trophy } from 'lucide-react';

import { ComingSoon } from '@/components/atoms/ComingSoon';
import { PageHeader } from '@/components/atoms/PageHeader';

const TITLE = 'Ranking';
const DESCRIPTION = 'Veja como você se posiciona contra o mundo.';

export const metadata = {
    title: 'Ranking · Xpeak',
};

export default function LeaderboardPage() {
    return (
        <main className="flex flex-1 flex-col gap-8 px-6 pt-10 pb-48">
            <PageHeader title={TITLE} description={DESCRIPTION} />
            <ComingSoon Icon={Trophy} />
        </main>
    );
}
