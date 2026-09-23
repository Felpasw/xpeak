'use client';

import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { Toaster } from 'sonner';

import { onUnauthorized } from '@/api';

export function Providers({ children }: PropsWithChildren) {
    const router = useRouter();

    const [client] = useState(
        () =>
            new QueryClient({
                defaultOptions: {
                    queries: { retry: false, refetchOnWindowFocus: false },
                    mutations: { retry: false },
                },
            }),
    );

    useEffect(() => {
        onUnauthorized(() => router.replace('/login'));
        return () => onUnauthorized(null);
    }, [router]);

    return (
        <QueryClientProvider client={client}>
            {children}
            <Toaster theme="dark" position="top-center" richColors />
        </QueryClientProvider>
    );
}
