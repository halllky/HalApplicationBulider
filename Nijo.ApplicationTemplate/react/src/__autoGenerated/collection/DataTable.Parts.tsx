import * as RT from '@tanstack/react-table'
import { DataTableColumn } from './DataTable.Public'

export const TABLE_ZINDEX = {
  CELLEDITOR: 30 as const,
  ROWHEADER_THEAD: 21 as const,
  THEAD: 20 as const,
  ROWHEADER_SELECTION: 11 as const,
  ROWHEADER: 10 as const,
  SELECTION: 1 as const,
}

/** Tanstack React Table の列定義 + DataTableコンポーネント独自のプロパティ */
export type RTColumnDefEx<TRow> = Omit<RT.ColumnDef<TRow>, 'cell' | 'header'> & {
  cell: Exclude<RT.ColumnDef<TRow>['cell'], undefined>
  header: string
  /** DataTableコンポーネント独自のプロパティ */
  ex: DataTableColumn<TRow>
}

export type CellPosition = {
  rowIndex: number
  colIndex: number
}

export type CellEditorRef<T> = {
  focus: () => void
  startEditing: (cell: RT.Cell<T, unknown>) => void
}
