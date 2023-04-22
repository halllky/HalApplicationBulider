import React from 'react';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels";
import { AppContextProvider } from './hooks/AppContext'
import { SideMenu } from './components/SideMenu';
import { Dashboard } from './components/Dashboard'
import { menuItems } from './__AutoGenerated';

function App() {

  return (
    <BrowserRouter>
      <AppContextProvider>
        <PanelGroup direction='horizontal'>
          <Panel defaultSize={20}>
            <SideMenu />
          </Panel>
          <PanelResizeHandle className='w-1' />
          <Panel>
            <Routes>
              <Route path='/' element={<Dashboard />} />
              {menuItems.map(item =>
                <Route key={item.url} path={item.url} element={item.el} />
              )}
              <Route path='*' element={<p>Not found.</p>} />
            </Routes>
          </Panel>
        </PanelGroup>
      </AppContextProvider>
    </BrowserRouter>
  );
}

export default App;
