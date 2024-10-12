import React from 'react'
import useEvent from 'react-use-event-hook'
import * as Util from './__autoGenerated/util'
import { AggregateOrMember, AggregateOrMemberWithId, PageState, PageStateFromServer } from './types'
import { UUID } from 'uuidjs'

/** nijo.exe 側との通信を行います。 */
export const useBackend = () => {
  // 通信準備完了したらtrue
  const [ready, setReady] = React.useState(false)

  // サーバー側ドメイン。
  // - nijo.exeから起動したときは '' (空文字)
  // - Nijoプロジェクトのデバッグプロファイル "nijo ui" が立ち上がっているときは https://localhost:8081 を画面から入力する
  const [backendDomain, setBackendDomain] = React.useState<string | undefined>('')
  const onChangBackendDomain = useEvent((value: string | undefined) => {
    if (value) {
      setBackendDomain(value)
      localStorage.setItem(STORAGE_KEY, value)
    } else {
      setBackendDomain(value)
      localStorage.removeItem(STORAGE_KEY)
    }
  })
  React.useLayoutEffect(() => {
    const savedValue = localStorage.getItem(STORAGE_KEY)
    if (savedValue) setBackendDomain(savedValue)
    setReady(true)
  }, [])

  const load = useEvent(async (): Promise<PageState> => {
    const response = await fetch(`${backendDomain ?? ''}/load`, {
      method: 'GET',
    })
    const responseBody = await response.json() as PageStateFromServer

    // 集約データは、サーバー上では入れ子のツリー構造。
    // クライアント側ではグリッドで取り回ししやすいように深さのプロパティをもったフラットな配列。
    const addId = (array: AggregateOrMember[] | undefined): AggregateOrMemberWithId[] => (array ?? []).map(agg => ({
      id: UUID.generate(),
      ...agg,
    }))
    const tree = Util.toTree(addId(responseBody.aggregates), {
      getId: agg => agg.id,
      getChildren: agg => addId(agg.children),
    })

    return {
      ...responseBody,
      aggregates: Util.flatten(tree),
    }
  })

  return {
    /** 画面初期表示時のみfalse。通信準備完了したらtrue */
    ready,
    /** バックエンド側ドメイン。末尾スラッシュなし。 */
    backendDomain,
    /** バックエンド側ドメインを設定します。 */
    onChangBackendDomain,
    /** 画面初期表示時データを読み込んで返します。 */
    load,
  }
}

const STORAGE_KEY = 'NIJO-UI::BACKEND-API'
