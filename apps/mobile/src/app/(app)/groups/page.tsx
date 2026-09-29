import { Users } from 'lucide-react';

import { ComingSoon } from '@/components/atoms/ComingSoon';
import { PageHeader } from '@/components/atoms/PageHeader';

const TITLE = 'Grupos';
const DESCRIPTION = 'Treine junto, empurre o parça, evolua em grupo.';

export const metadata = {
    title: 'Grupos · Xpeak',
};

export default function GroupsPage() {
    return (
        <main className="flex flex-1 flex-col gap-8 px-6 pt-10 pb-48">
            <PageHeader title={TITLE} description={DESCRIPTION} />
            <ComingSoon Icon={Users} />
        </main>
    );
}
