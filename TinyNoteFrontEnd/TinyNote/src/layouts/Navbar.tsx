import { AppBar, Toolbar, Typography } from '@mui/material'
import { Link } from 'react-router-dom'

export default function Navbar() {
    return (
        <AppBar position="fixed">
        <Toolbar sx={{ display: 'flex', justifyContent: 'space-between' }}>
            <Typography 
              variant="h6" 
              component={Link}
              to="/"
              sx={{ 
                textDecoration: 'none', 
                color: 'inherit',
                '&:hover': {
                  opacity: 0.8
                }
              }}
            >
              TinyNote
            </Typography>
            <Typography 
              variant="h6" 
              component={Link} 
              to="/reservation" 
              sx={{ 
                textDecoration: 'none', 
                color: 'inherit', 
                '&:hover': { 
                  opacity: 0.8 
                } 
              }}
            >
              Add a Note
            </Typography>  
        </Toolbar>
    </AppBar>
    );
}