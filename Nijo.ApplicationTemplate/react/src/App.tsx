import { useMemo } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
// import { DefaultNijoApp } from './__autoGenerated'
import UserInputテスト from './debug-room/UserInputテスト'
// import BatchUpdateテスト from './debug-room/BatchUpdateテスト'
import VFormテスト from './debug-room/VFromテスト'
import CssGrid挙動調査 from './debug-room/CssGrid挙動調査'
import TabLayoutテスト from './debug-room/TabLayoutテスト'
import UnknownObjectViewerテスト from './debug-room/UnknownObjectViewerテスト'
import MultiViewEditable検討 from './debug-room/MultiViewEditable検討'
import { AutoGeneratedCustomizer } from './__autoGenerated'

function App() {

  // デバッグ用の画面を追加する場合はここに足す
  const debugRooms = useMemo<{ path: string, label: string, element: JSX.Element }[]>(() => [
    { path: '/input', label: 'UserInputテスト', element: <UserInputテスト /> },
    // { path: '/batch-update', label: 'BatchUpdateテスト', element: <BatchUpdateテスト /> },
    { path: '/vform', label: 'VFormテスト', element: <VFormテスト /> },
    { path: '/css-grid', label: 'CssGrid挙動調査', element: <CssGrid挙動調査 /> },
    { path: '/tab-layout', label: 'TabLayoutテスト', element: <TabLayoutテスト /> },
    { path: '/unknown-object-viewer', label: 'UnknownObjectViewerテスト', element: <UnknownObjectViewerテスト /> },
    { path: '/multi-view-editable', label: '一括編集画面', element: <MultiViewEditable検討 /> },
  ], [])

  return (
    // <DefaultNijoApp />

    // デバッグ用の画面
    <BrowserRouter>
      <Routes>
        {debugRooms.map(debugRoom => (
          <Route key={debugRoom.path} {...debugRoom} />
        ))}
        <Route path="*" element={(
          <div className="flex flex-col p-2 gap-2">
            {debugRooms.map(debugRoom => (
              <a key={debugRoom.path} href={debugRoom.path} className="underline text-sky-700">
                {debugRoom.label}
              </a>
            ))}
          </div>
        )} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
