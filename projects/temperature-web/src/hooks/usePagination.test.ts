import { act, renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { usePagination } from './usePagination'

describe('usePagination', () => {
  it('starts at page 1 by default', () => {
    const { result } = renderHook(() => usePagination())
    expect(result.current.page).toBe(1)
  })

  it('respects initialPage option', () => {
    const { result } = renderHook(() => usePagination({ initialPage: 3 }))
    expect(result.current.page).toBe(3)
  })

  it('nextPage increments by 1', () => {
    const { result } = renderHook(() => usePagination())
    act(() => result.current.nextPage())
    expect(result.current.page).toBe(2)
  })

  it('prevPage decrements by 1', () => {
    const { result } = renderHook(() => usePagination({ initialPage: 3 }))
    act(() => result.current.prevPage())
    expect(result.current.page).toBe(2)
  })

  it('prevPage does not go below 1', () => {
    const { result } = renderHook(() => usePagination())
    act(() => result.current.prevPage())
    expect(result.current.page).toBe(1)
  })

  it('goToPage sets exact page', () => {
    const { result } = renderHook(() => usePagination())
    act(() => result.current.goToPage(7))
    expect(result.current.page).toBe(7)
  })

  it('reset returns to initialPage', () => {
    const { result } = renderHook(() => usePagination({ initialPage: 2 }))
    act(() => result.current.goToPage(9))
    act(() => result.current.reset())
    expect(result.current.page).toBe(2)
  })

  it('uses default pageSize 10', () => {
    const { result } = renderHook(() => usePagination())
    expect(result.current.pageSize).toBe(10)
  })

  it('respects custom pageSize', () => {
    const { result } = renderHook(() => usePagination({ pageSize: 25 }))
    expect(result.current.pageSize).toBe(25)
  })
})
