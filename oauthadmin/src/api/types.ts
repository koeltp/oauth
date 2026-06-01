export interface SearchPager<T = any> {
  pageIndex?: number
  pageSize?: number
  condition?: T
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageIndex: number
  pageSize: number
}