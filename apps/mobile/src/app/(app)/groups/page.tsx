'use client';

import { GLOBAL_GROUP_ID } from '@xpeak/shared';

import { PageHeader } from '@/components/atoms/PageHeader';
import { CheckinList } from '@/components/organisms/CheckinList';

const TITLE = 'Grupos';
const DESCRIPTION = 'Histórico de check-ins do grupo';

export default function GroupsPage() {
    return (
        <main className="flex flex-1 flex-col gap-8 px-6 pt-10 pb-48">
            <PageHeader title={TITLE} description={DESCRIPTION} />
            <CheckinList groupId={GLOBAL_GROUP_ID} />
        </main>
    );
}
