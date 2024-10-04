import React, { useCallback, useEffect, useImperativeHandle, useMemo, useRef, useState } from "react"
import { ChevronUpDownIcon } from "@heroicons/react/24/solid"
import { useIMEOpened, normalize, useOutsideClick } from "../util"
import { TextInputBase, TextInputBaseAdditionalRef } from "./TextInputBase"
import { CustomComponentProps, CustomComponentRef, SyncComboProps, defineCustomComponent } from "./InputBase"
import useEvent from "react-use-event-hook"
import { useDialogContext } from "../collection"

export const ComboBoxBase = defineCustomComponent(<TOption, TEmitValue, TMatchingKey extends string = string>(
  props2: CustomComponentProps<TEmitValue, SyncComboProps<TOption, TEmitValue, TMatchingKey>>,
  ref: React.ForwardedRef<CustomComponentRef<TEmitValue>>
) => {
  const {
    options,
    matchingKeySelectorFromOption,
    matchingKeySelectorFromEmitValue,
    emitValueSelector,
    textSelector,
    value,
    onChange,
    onBlur,
    onKeyDown,
    readOnly,
    onKeywordChanged,
    name,
    dropdownAutoOpen,
  } = props2

  const dropdownRef = useRef<DropDownApi>(null)

  // フィルタリング
  const [keyword, setKeyword] = useState<string | undefined>(undefined) // フォーカスを当ててから何か入力された場合のみundefinedでなくなる
  const filtered = useMemo(() => {
    if (keyword === undefined) return [...options]
    const normalized = normalize(keyword)
    if (!normalized) return [...options]
    return options.filter(item => textSelector(item).includes(normalized))
  }, [options, keyword, textSelector])

  // リストのカーソル移動
  const [highlighted, setHighlightItem] = useState<TMatchingKey>()
  const highlightAnyItem = useCallback(() => {
    if (value) {
      setHighlightItem(matchingKeySelectorFromEmitValue(value))
    } else if (filtered.length > 0) {
      setHighlightItem(matchingKeySelectorFromOption(filtered[0]))
    } else {
      setHighlightItem(undefined)
    }
  }, [value, filtered, matchingKeySelectorFromOption, matchingKeySelectorFromEmitValue])
  const highlightUpItem = useCallback(() => {
    const currentIndex = filtered.findIndex(item => matchingKeySelectorFromOption(item) === highlighted)
    if (currentIndex === -1) {
      if (filtered.length > 0) setHighlightItem(matchingKeySelectorFromOption(filtered[0]))
    } else if (currentIndex > 0) {
      setHighlightItem(matchingKeySelectorFromOption(filtered[currentIndex - 1]))
    }
  }, [filtered, highlighted, matchingKeySelectorFromOption])
  const highlightDownItem = useCallback(() => {
    const currentIndex = filtered.findIndex(item => matchingKeySelectorFromOption(item) === highlighted)
    if (currentIndex === -1) {
      if (filtered.length > 0) setHighlightItem(matchingKeySelectorFromOption(filtered[0]))
    } else if (currentIndex < (filtered.length - 1)) {
      setHighlightItem(matchingKeySelectorFromOption(filtered[currentIndex + 1]))
    }
  }, [filtered, highlighted, matchingKeySelectorFromOption])

  // 選択
  const selectItemByValue = useCallback((value: string | undefined) => {
    const foundItem = options.find(item => matchingKeySelectorFromOption(item) === value)
    const emitValue = foundItem ? emitValueSelector(foundItem) : undefined
    setHighlightItem(foundItem ? matchingKeySelectorFromOption(foundItem) : undefined)
    setKeyword(undefined)
    onChange?.(emitValue)
  }, [options, onChange, matchingKeySelectorFromOption, emitValueSelector])

  // 入力中のテキストに近い最も適当な要素を取得する
  const getHighlightedOrAnyItem = useCallback((): TOption | undefined => {
    if (highlighted && (dropdownAutoOpen || dropdownRef.current?.isOpened)) {
      const found = filtered.find(item => matchingKeySelectorFromOption(item) === highlighted)
      if (found) return found
    }
    if (keyword === undefined && value) {
      const keyOfValue = matchingKeySelectorFromEmitValue(value)
      return filtered.find(x => matchingKeySelectorFromOption(x) === keyOfValue)
    }
    if (!dropdownAutoOpen && dropdownRef.current?.isOpened === false) return undefined
    if (keyword && normalize(keyword) === '') return undefined
    if (filtered.length > 0) return filtered[0]
    return undefined
  }, [value, keyword, filtered, highlighted, matchingKeySelectorFromOption, matchingKeySelectorFromEmitValue, dropdownAutoOpen])

  // events
  const onChangeKeyword = useCallback((value: string | undefined) => {
    dropdownRef.current?.open()
    setKeyword(value)
    onKeywordChanged?.(value)
  }, [onKeywordChanged])

  const [{ isImeOpen }] = useIMEOpened()
  const handleKeyDown: React.KeyboardEventHandler<HTMLInputElement> = useCallback(e => {
    if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
      // ドロップダウンを開く
      if (!dropdownAutoOpen && !isImeOpen && !dropdownRef.current?.isOpened) {
        dropdownRef.current?.open()
        highlightAnyItem()
        e.preventDefault()
        return
      }

      // 上下移動
      if (e.key === 'ArrowUp') {
        highlightUpItem()
      } else {
        highlightDownItem()
      }
      e.preventDefault()
    }
    // ドロップダウン中のハイライトが当たっている要素の選択を確定する
    else if (e.key === 'Enter'
      && !isImeOpen
      && (dropdownAutoOpen || dropdownRef.current?.isOpened)
    ) {
      const anyItem = getHighlightedOrAnyItem()
      const valueOfAnyItem = anyItem ? emitValueSelector(anyItem) : undefined
      onChange?.(valueOfAnyItem)
      setKeyword(undefined)
      setHighlightItem(undefined)
      dropdownRef.current?.close()
      e.preventDefault()
    }
    // 任意の処理
    else {
      onKeyDown?.(e)
    }
  }, [isImeOpen, getHighlightedOrAnyItem, highlightAnyItem, highlightUpItem, highlightDownItem, onChange, onKeyDown, emitValueSelector, dropdownAutoOpen])

  const onClickItem: React.MouseEventHandler<HTMLLIElement> = useCallback(e => {
    selectItemByValue((e.target as HTMLLIElement).getAttribute('value') as string)
    setHighlightItem(undefined)
    dropdownRef.current?.close()
  }, [selectItemByValue])

  const onDropdownOpened = useCallback(() => {
    highlightAnyItem()
    textBaseRef.current?.focus()
  }, [highlightAnyItem])

  const textBaseRef = useRef<CustomComponentRef & TextInputBaseAdditionalRef>(null)
  useImperativeHandle(ref, () => ({
    getValue: () => {
      const anyItem = getHighlightedOrAnyItem()
      const valueOfAnyItem = anyItem ? emitValueSelector(anyItem) : undefined
      return valueOfAnyItem
    },
    focus: opt => textBaseRef.current?.focus(opt),
  }), [getHighlightedOrAnyItem, emitValueSelector, textBaseRef])

  const displayText = useMemo(() => {
    if (keyword !== undefined) return keyword
    if (value !== undefined) {
      const keyOfValue = matchingKeySelectorFromEmitValue(value)
      const valueFromOptions = options.find(x => matchingKeySelectorFromOption(x) === keyOfValue)
      return valueFromOptions ? textSelector(valueFromOptions) : keyOfValue
    }
    return ''
  }, [keyword, value, textSelector, matchingKeySelectorFromOption, matchingKeySelectorFromEmitValue, options])

  const [, dispatchDialog] = useDialogContext()
  const openDropdown = useEvent(() => {
    dispatchDialog(state => state.openPopup(textBaseRef.current?.element, () => (
      <ul>
        {filtered.length === 0 && (
          <ListItem className="text-color-6">データなし</ListItem>
        )}
        {filtered.map(item => (
          <ListItem
            key={matchingKeySelectorFromOption(item)}
            value={matchingKeySelectorFromOption(item)}
            active={matchingKeySelectorFromOption(item) === highlighted}
            onClick={onClickItem}
          >
            {textSelector(item)}&nbsp;
          </ListItem>
        ))}
      </ul>
    )))
  })

  return (
    <TextInputBase
      ref={textBaseRef}
      readOnly={readOnly}
      name={name}
      value={displayText}
      onOneCharChanged={onChangeKeyword}
      onKeyDown={handleKeyDown}
      AtEnd={<DropdownButton onClick={openDropdown} />}
    />
  )
})

const DropdownButton = ({ onClick }: {
  onClick?: () => void
}) => {
  return (
    <ChevronUpDownIcon
      className="w-6 text-color-5 border-l border-color-5 cursor-pointer"
      onClick={onClick}
    />
  )
}

export type DropDownBody = (props: { focusRef: React.RefObject<never> }) => React.ReactNode
export type DropDownApi = { isOpened: boolean, open: () => void, close: () => void }

const Dropdown = ({ onClose, children }: {
  onClose?: () => void
  children?: DropDownBody
}) => {
  const divRef = useRef<HTMLDivElement>(null)
  const focusRef = useRef<never | null>(null)

  useEffect(() => {
    // ドロップダウン内の要素にフォーカスを当てる
    const htmlElement = focusRef.current as { focus: () => void } | null
    if (typeof htmlElement?.focus === 'function') {
      htmlElement.focus()
    }
  }, [])

  useOutsideClick(divRef, () => {
    onClose?.()
  }, [onClose])

  const onBlur: React.FocusEventHandler = useEvent(e => {
    onClose?.()
  })
  const onKeyDown: React.KeyboardEventHandler = useEvent(e => {
    if (e.key === 'Escape') {
      onClose?.()
      e.preventDefault()
    }
  })

  return (
    <div
      ref={divRef}
      className="absolute top-[calc(100%+2px)] left-[-1px] min-w-[calc(100%+2px)] z-10 bg-color-base border border-color-5 outline-none"
      onBlur={onBlur}
      onKeyDown={onKeyDown}
    >
      {children?.({ focusRef })}
    </div>
  )
}

const ListItem = (props: React.LiHTMLAttributes<HTMLLIElement> & {
  active?: boolean
}) => {
  const {
    active,
    children,
    className,
    ...rest
  } = props

  const lighlight = active ? 'bg-color-4' : ''

  return (
    <li {...rest} className={`cursor-pointer ${lighlight} ${className}`}>
      {children}
    </li>
  )
}
