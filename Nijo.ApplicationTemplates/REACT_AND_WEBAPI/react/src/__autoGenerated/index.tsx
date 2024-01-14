import React from 'react'
import { BrowserRouter, Link, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from 'react-query'
import * as Icon from '@heroicons/react/24/outline'
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels'
import * as Util from './util'
import * as AutoGenerated from './autogenerated-menu'

export * from './collection'
export * from './input'
export * from './util'

import './nijo-default-style.css'
import 'ag-grid-community/styles/ag-grid.css'
import 'ag-grid-community/styles/ag-theme-alpine.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false,
    },
  },
})

function ApplicationRootInContext({ children }: {
  children?: React.ReactNode
}) {
  const { data: { darkMode } } = Util.useUserSetting()

  return (
    <PanelGroup
      direction='horizontal'
      autoSaveId="LOCAL_STORAGE_KEY.SIDEBAR_SIZE_X"
      className={darkMode ? 'dark' : undefined}>

      {/* サイドメニュー */}
      <Panel defaultSize={20}>
        <PanelGroup direction="vertical"
          className="bg-color-gutter text-color-12"
          autoSaveId="LOCAL_STORAGE_KEY.SIDEBAR_SIZE_Y" >
          <Panel className="flex flex-col">
            <Link to='/' className="p-1 ellipsis-ex font-semibold select-none" >
              {AutoGenerated.THIS_APPLICATION_NAME}
            </Link>
            <nav className="flex-1 overflow-y-auto leading-none">
              {
                AutoGenerated.menuItems.map(item =>
                  <SideMenuLink key={item.url} url={item.url} > {item.text} </SideMenuLink>
                )
              }
            </nav>
          </Panel>

          <PanelResizeHandle className="h-1 border-b border-color-5" />

          <Panel className="flex flex-col">
            <nav className="flex-1 overflow-y-auto leading-none">
              <SideMenuLink url="/settings" icon={Icon.Cog8ToothIcon} > 設定 </SideMenuLink>
            </nav>
            <span className="p-1 text-sm whitespace-nowrap overflow-hidden">
              ver. 0.9.0.0
            </span>
          </Panel>
        </PanelGroup>
      </Panel>

      <PanelResizeHandle className='w-1 bg-color-base' />

      {/* コンテンツ */}
      <Panel className={`flex flex-col [&>:first-child]:flex-1 pr-1 pt-1 pb-1 bg-color-base text-color-12`}>
        <Routes>
          <Route path='/' element={<> </>} />
          <Route path='/settings' element={< Util.ServerSettingScreen />} />
          {
            AutoGenerated.routes.map(route =>
              <Route key={route.url} path={route.url} element={route.el} />
            )
          }
          {children}
          <Route path='*' element={<p> Not found.</p>} />
        </Routes>

        <Util.InlineMessageList />
      </Panel>
    </PanelGroup>
  )
}

export function DefaultNijoApp({ children }: {
  children?: React.ReactNode
}) {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Util.MsgContextProvider>
          <Util.UserSettingContextProvider>
            <ApplicationRootInContext>
              {children}
            </ApplicationRootInContext>
            <Util.Toast />
          </Util.UserSettingContextProvider>
        </Util.MsgContextProvider>
      </BrowserRouter>
    </QueryClientProvider >
  )
}

const SideMenuLink = ({ url, icon, children }: {
  url: string
  icon?: React.ElementType
  children?: React.ReactNode
}) => {

  const location = useLocation()
  const className = location.pathname.startsWith(url)
    ? 'outline-none inline-block w-full p-1 ellipsis-ex font-bold bg-color-base'
    : 'outline-none inline-block w-full p-1 ellipsis-ex'

  return (
    <NavLink to={url} className={className} >
      {React.createElement(icon ?? Icon.CircleStackIcon, { className: 'inline w-4 mr-1 opacity-70 align-middle' })}
      <span className="text-sm align-middle select-none" > {children} </span>
    </NavLink>
  )
}
