import * as Util from './__autoGenerated/util'

/** ページ全体の状態（サーバーから送られてきた生の状態） */
export type PageState = {
  /** プロジェクトのルートディレクトリ */
  projectRoot: string | undefined
  /** 編集対象XMLファイル名 */
  editingXmlFilePath: string | undefined
  /** 集約など */
  aggregates: AggregateOrMember[] | undefined
  /** 集約やメンバーの型定義 */
  aggregateOrMemberTypes: AggregateOrMemberTypeDef[] | undefined
  /** オプショナル属性定義 */
  optionalAttributes: OptionalAttributeDef[] | undefined
}
export const getEmptyPageState = (): PageState => ({
  aggregateOrMemberTypes: undefined,
  aggregates: undefined,
  editingXmlFilePath: undefined,
  optionalAttributes: undefined,
  projectRoot: undefined,
})

/** クライアントからサーバーへ送るデータ（バリデーション時や保存時） */
export type ClientRequest = {
  /** 集約など */
  aggregates: AggregateOrMember[] | undefined
}

/** 集約またはメンバー。グリッド行表示に特化した形 */
export type GridRow = Util.TreeNode<AggregateOrMember>

/** 集約または集約メンバー */
export type AggregateOrMember = {
  /** UI上で取りまわすための一時的に付与されるID。既存データはサーバー上で、新規データはクライアント側で発番される。 */
  uniqueId: string
  /** 集約または集約メンバーの画面表示名 */
  displayName: string | undefined
  /** 集約または集約メンバーの型 */
  type: AggregateOrMemberTypeKey | undefined
  /** ref-to や enum の参照先。または step や variation-item の番号 */
  typeDetail: string | undefined
  /** オプショナル属性の値 */
  attrValues: OptionalAttributeValue[] | undefined
  /** 直近の子要素。計算コストの都合で画面表示時と保存時のみ更新される想定 */
  children: AggregateOrMember[] | undefined
  /** 備考 */
  comment: string | undefined
}

/** 集約または集約メンバーのオプショナル属性の値 */
export type OptionalAttributeValue = {
  key: OptionalAttributeKey
  /** オプショナル属性の値 */
  value: string | undefined
}

/** 集約または集約メンバーの型定義。`read-model-2`, `ref-to:`, `word`, ... など */
export type AggregateOrMemberTypeDef = {
  key: AggregateOrMemberTypeKey
  /** 画面表示名。XMLのis属性の名称との変換はサーバー側で行う。 */
  displayName: string | undefined
  /** この種類の説明文 */
  helpText: string | undefined
  /** variation-item や step の場合は区分値やステップ番号を指定する必要があるので */
  requiredNumberValue: boolean | undefined
}

/** オプショナル属性定義。DbName, key, required, ... など */
export type OptionalAttributeDef = {
  key: OptionalAttributeKey
  /** 画面表示名。XMLのis属性の名称との変換はサーバー側で行う。 */
  displayName: string | undefined
  /** この属性の説明文 */
  helpText: string | undefined
  /** 種類 */
  type: 'string' | 'number' | 'boolean' | undefined
}


const s1: unique symbol = Symbol()
const s2: unique symbol = Symbol()
/** 型定義のキー */
export type AggregateOrMemberTypeKey
  = string & { [s1]: never } // Write Model なら 'w' といったように決め打ちの文字列。サーバー側で定義。
  | `ref-to:${string}` // 参照先の場合はこちら。コロンの後ろは画面上でのみ使用される一時的なUUID
  | `enum:${string}` // enumの場合はこちら。コロンの後ろは画面上でのみ使用される一時的なUUID
/** オプショナル属性のキー */
export type OptionalAttributeKey = string & { [s2]: never }

/** ref-to: の参照先の候補 */
export type RefToAggregateOption = {
  /** "ref-to:画面上でのみ使用される参照先集約のUUID" */
  key: `ref-to:${string}`
  /** "ref-to:画面上の表示名" */
  displayName: `ref-to:${string}` | undefined
}

/** enumの参照先候補 */
export type EnumOption = {
  /** "enum:画面上でのみ使用される当該列挙体のUUID" */
  key: `enum:${string}`
  /** "画面上の表示名" */
  displayName: string | undefined
}
