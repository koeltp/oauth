import { ref } from 'vue'

export const PAGE_SIZES = [10, 20, 50, 100]

export function usePagination(loadFn: () => Promise<void>) {
  const pageIndex = ref(1)
  const pageSize = ref(10)
  const totalCount = ref(0)

  const onPageSizeChange = () => {
    pageIndex.value = 1
    loadFn()
  }

  const onPageChange = () => {
    loadFn()
  }

  return {
    pageIndex,
    pageSize,
    totalCount,
    onPageSizeChange,
    onPageChange
  }
}