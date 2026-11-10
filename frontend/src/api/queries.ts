import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { POLL_INTERVAL_MS } from '../config'
import { api } from './client'
import type { CreateClassInput } from './types'

export const queryKeys = {
  classes: ['classes'] as const,
  hold: (holdId: string) => ['hold', holdId] as const,
  myHolds: (studentId: string) => ['myHolds', studentId] as const,
}

/** All classes with seat counts; refreshed every few seconds so numbers stay live. */
export function useClasses() {
  return useQuery({
    queryKey: queryKeys.classes,
    queryFn: api.listClasses,
    refetchInterval: POLL_INTERVAL_MS,
  })
}

export function useCreateClass() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: CreateClassInput) => api.createClass(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.classes }),
  })
}

export function useApplyForSeat(studentId: string | null) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (classId: string) => api.applyForSeat(classId, studentId ?? ''),
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.classes })
      if (studentId) queryClient.invalidateQueries({ queryKey: queryKeys.myHolds(studentId) })
    },
  })
}
