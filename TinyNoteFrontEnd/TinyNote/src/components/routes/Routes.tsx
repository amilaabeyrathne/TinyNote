import { createBrowserRouter } from "react-router-dom";
import Notes from "../notes/Notes";
import App from '../../App';

export const router = createBrowserRouter([
    {
        path: '/',
        element: <App />,
        children: [
            {
                path: '/',
                element: <Notes />,
            }
        ]
    }
    
]);