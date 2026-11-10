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

/** One application; polled so a change made elsewhere (another tab) shows up here too. */
export function useHold(holdId: string) {
  return useQuery({
    queryKey: queryKeys.hold(holdId),
    queryFn: () => api.getHold(holdId),
    refetchInterval: POLL_INTERVAL_MS,
  })
}

/** All applications of the current student. */
export function useMyHolds(studentId: string | null) {
  return useQuery({
    queryKey: queryKeys.myHolds(studentId ?? ''),
    queryFn: () => api.myHolds(studentId!),
    enabled: studentId !== null,
    refetchInterval: POLL_INTERVAL_MS,
  })
}

/** Refresh everything a hold change affects: the hold, the student's list, seat counts. */
function useRefreshAfterHoldChange() {
  const queryClient = useQueryClient()
  return (holdId: string, studentId: string) => {
    queryClient.invalidateQueries({ queryKey: queryKeys.hold(holdId) })
    queryClient.invalidateQueries({ queryKey: queryKeys.myHolds(studentId) })
    queryClient.invalidateQueries({ queryKey: queryKeys.classes })
  }
}

export function useConfirmHold(studentId: string | null) {
  const queryClient = useQueryClient()
  const refresh = useRefreshAfterHoldChange()
  return useMutation({
    mutationFn: (holdId: string) => api.confirmHold(holdId, studentId ?? ''),
    // Show CONFIRMED immediately using the server's answer, then refresh the rest.
    onSuccess: (hold, holdId) => queryClient.setQueryData(queryKeys.hold(holdId), hold),
    onSettled: (_data, _error, holdId) => refresh(holdId, studentId ?? ''),
  })
}

export function useCancelHold(studentId: string | null) {
  const refresh = useRefreshAfterHoldChange()
  return useMutation({
    mutationFn: (holdId: string) => api.cancelHold(holdId, studentId ?? ''),
    onSettled: (_data, _error, holdId) => refresh(holdId, studentId ?? ''),
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
