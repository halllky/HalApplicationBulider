import React, { useState } from 'react'
import useEvent from 'react-use-event-hook'
import { useFieldArray, FormProvider, useWatch } from 'react-hook-form'
import * as Icon from '@heroicons/react/24/outline'
import { Panel, PanelGroup, PanelResizeHandle, ImperativePanelHandle } from 'react-resizable-panels'
import * as Layout from '../__autoGenerated/collection'
import * as Input from '../__autoGenerated/input'
import * as Util from '../__autoGenerated/util'

type TestData = {
  state?: 'ADD' | 'MOD' | 'DEL' | 'NONE'
  id?: string
  name?: string
  age?: number
  details: { item?: string, price?: number }[]
}
const getTestData = (count: number): TestData[] => {
  const states = ['ADD', 'MOD', 'DEL', 'NONE'] as const
  return Array.from({ length: count }, (_, i) => ({
    state: states[Math.floor(Math.random() * states.length)],
    id: i.toString(),
    name: `Name ${i}`,
    age: Math.floor(Math.random() * 100),
    details: [{ item: 'ITEM1', price: 100 }, { item: 'ITEM2', price: 200 }],
  }))
}

export default function () {
  const [loaded, setLoaded] = React.useState(false)
  const [data, setData] = React.useState<TestData[]>([])
  React.useEffect(() => {
    setData(getTestData(200))
    setLoaded(true)
  }, [])

  return loaded ? (
    <AfterLoaded data={data} />
  ) : (
    <Input.NowLoading />
  )
}

const AfterLoaded = ({ data }: {
  data: TestData[]
}) => {
  const tableRef = React.useRef<Layout.DataTableRef<TestData>>(null)

  // データ
  const reactHookFormMethods = Util.useFormEx<{ data: TestData[] }>({ defaultValues: { data } })
  const { insert, remove } = useFieldArray({ name: 'data', control: reactHookFormMethods.control })
  const fields2 = useWatch({ name: 'data', control: reactHookFormMethods.control })

  // 一覧部分と詳細部分のレイアウト
  const [singleViewPosition, setSingleViewPosition] = React.useState<'horizontal' | 'vertical'>('horizontal')
  const [singleViewCollapsed, setSingleViewCollapsed] = React.useState(false)
  const resizerCssClass = React.useMemo(() => {
    return singleViewPosition === 'horizontal' ? 'w-2' : 'h-2'
  }, [singleViewPosition])
  const singleViewRef = React.useRef<ImperativePanelHandle>(null)
  const handleClickHorizontal = useEvent(() => {
    setSingleViewPosition('horizontal')
    singleViewRef.current?.expand()
  })
  const handleClickVertical = useEvent(() => {
    setSingleViewPosition('vertical')
    singleViewRef.current?.expand()
  })
  const handleClickCollapse = useEvent(() => {
    singleViewRef.current?.collapse()
  })

  // 列定義
  const columns = React.useMemo((): Layout.DataTableColumn<TestData>[] => [{
    id: 'col-1',
    header: '',
    render: row => <Layout.AddModDelStateCell state={row.state} />,
    onClipboardCopy: row => row.state ?? '',
    defaultWidthPx: 48,
    fixedWidth: true,
  }, {
    id: 'col0',
    header: 'ID',
    render: row => <ReadOnlyCell>{row.id}</ReadOnlyCell>,
    onClipboardCopy: row => row.id ?? '',
  }, {
    id: 'col1',
    header: '名前',
    render: row => <ReadOnlyCell>{row.name}</ReadOnlyCell>,
    onClipboardCopy: row => row.name ?? '',
  }, {
    id: 'col2',
    header: '年齢',
    render: row => <ReadOnlyCell>{row.age}</ReadOnlyCell>,
    onClipboardCopy: row => row.age?.toString() ?? '',
  }], [])

  // 選択されている行
  const [activeRowIndex, setActiveRowIndex] = useState<number | undefined>(undefined)
  const { debouncedValue: debouncedActiveRowIndex, debouncing } = Util.useDebounce(activeRowIndex, 300)
  const handleActiveRowChanged = useEvent((e: { getRow: () => TestData, rowIndex: number } | undefined) => {
    setActiveRowIndex(e?.rowIndex)
  })

  // 行追加
  const handleInsert = useEvent(() => {
    insert(activeRowIndex ?? 0, { state: 'ADD', details: [] })
  })

  // 行削除
  const handleRemove = useEvent(() => {
    if (!tableRef.current) return
    const removeIndexes: number[] = []
    for (const x of tableRef.current.getSelectedRows()) {
      if (x.row.state === 'ADD') {
        removeIndexes.push(x.rowIndex)
      } else {
        reactHookFormMethods.setValue(`data.${x.rowIndex}.state`, 'DEL')
      }
    }
    remove(removeIndexes)
  })

  // リセット
  const handleReset = useEvent(() => {
    if (!confirm('選択されている行の変更を元に戻しますか？')) return
  })

  return (
    <FormProvider {...reactHookFormMethods}>
      <Layout.PageFrame
        header={<>
          <Layout.PageTitle>一括編集画面試作</Layout.PageTitle>
          <Input.IconButton onClick={handleInsert} outline mini>追加</Input.IconButton>
          <Input.IconButton onClick={handleRemove} outline mini>削除</Input.IconButton>
          <Input.IconButton onClick={handleReset} outline mini>リセット</Input.IconButton>
          <div className="flex-1"></div>
          <div className="self-stretch flex gap-1 border border-color-4">
            <Input.IconButton icon={Icon.ArrowsRightLeftIcon} hideText className="p-2" onClick={handleClickHorizontal} fill={!singleViewCollapsed && singleViewPosition === 'horizontal'}>左右に並べる</Input.IconButton>
            <Input.IconButton icon={Icon.ArrowsUpDownIcon} hideText className="p-2" onClick={handleClickVertical} fill={!singleViewCollapsed && singleViewPosition === 'vertical'}>上下に並べる</Input.IconButton>
            <Input.IconButton icon={Icon.ArrowsPointingOutIcon} hideText className="p-2" onClick={handleClickCollapse} fill={singleViewCollapsed}>一覧のみ表示</Input.IconButton>
          </div>
          <div className="basis-4"></div>
          <Input.IconButton fill>保存</Input.IconButton>
        </>}
      >
        <PanelGroup direction={singleViewPosition}>

          {/* 一覧欄 */}
          <Panel className="border border-color-4">
            <Layout.DataTable
              ref={tableRef}
              data={fields2}
              columns={columns}
              onActiveRowChanged={handleActiveRowChanged}
              className="h-full"
            />
          </Panel>

          <PanelResizeHandle className={resizerCssClass} />

          {/* 詳細欄 */}
          <Panel ref={singleViewRef} collapsible onCollapse={setSingleViewCollapsed} className="relative border border-color-4">
            {!singleViewCollapsed && (
              <DetailView
                activeRowIndex={debouncedActiveRowIndex}
              />
            )}
            {debouncing && (
              <Input.NowLoading />
            )}
          </Panel>
        </PanelGroup>
      </Layout.PageFrame>
    </FormProvider>
  )
}

const DetailView = ({ activeRowIndex }: {
  activeRowIndex: number | undefined
}) => {
  return activeRowIndex === undefined ? (
    <></>
  ) : (
    <Root activeRowIndex={activeRowIndex} />
  )
}

const Root = ({ activeRowIndex }: {
  activeRowIndex: number
}) => {
  const { registerEx, control } = Util.useFormContextEx<{ data: TestData[] }>()

  // 詳細
  const { update } = useFieldArray({ name: `data.${activeRowIndex}.details`, control })
  const detail = useWatch({ name: `data.${activeRowIndex}.details`, control })
  const columns = React.useMemo((): Layout.DataTableColumn<TestData['details'][0]>[] => [{
    id: 'col0',
    header: 'ITEM',
    render: row => <ReadOnlyCell>{row.item}</ReadOnlyCell>,
    onClipboardCopy: row => row.item ?? '',
  }, {
    id: 'col1',
    header: '価格',
    render: row => <ReadOnlyCell>{row.price}</ReadOnlyCell>,
    onClipboardCopy: row => row.price?.toString() ?? '',
    editSetting: {
      type: 'text',
      onStartEditing: row => row.price?.toString(),
      onEndEditing: (row, value) => {
        if (value === undefined) {
          row.price = undefined
        } else {
          const parsed = Util.tryParseAsNumberOrEmpty(value)
          row.price = parsed.ok ? parsed.num : undefined
        }
      },
      onClipboardPaste: (row, value) => {
        if (value === undefined) {
          row.price = undefined
        } else {
          const parsed = Util.tryParseAsNumberOrEmpty(value)
          row.price = parsed.ok ? parsed.num : undefined
        }
      },
    },
  }], [])

  return (
    <div className="grid grid-cols-[5rem,1fr] gap-px">
      <div className="text-right pr-2">
        ID
      </div>
      <div>
        <Input.Word {...registerEx(`data.${activeRowIndex}.id`)} />
      </div>
      <div className="text-right pr-2">
        名前
      </div>
      <div>
        <Input.Word {...registerEx(`data.${activeRowIndex}.name`)} />
      </div>
      <div className="text-right pr-2">
        年齢
      </div>
      <div>
        <Input.Num {...registerEx(`data.${activeRowIndex}.age`)} />
      </div>
      <div className="col-span-2">
        <Layout.DataTable
          data={detail}
          columns={columns}
          onChangeRow={update}
          className="min-h-16"
        />
      </div>
    </div>
  )
}

const ReadOnlyCell = ({ children }: {
  children?: React.ReactNode
}) => {
  return (
    <span className="block w-full px-1 overflow-hidden whitespace-nowrap">
      {children}
      &nbsp;
    </span>
  )
}
