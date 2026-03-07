import { Outlet } from 'react-router-dom'
import { ToastContainer } from 'react-toastify'
import 'react-toastify/dist/ReactToastify.css'
import Navbar from './layouts/Navbar'
import { Container } from '@mui/material'
import './App.css'

function App() {
  return (
    <>
      <Navbar />
      <Container maxWidth="xl" sx={{  mt: 0, mb: 4 }}>
        <Outlet />
      </Container>
      <ToastContainer />
    </>
  )
}

export default App
