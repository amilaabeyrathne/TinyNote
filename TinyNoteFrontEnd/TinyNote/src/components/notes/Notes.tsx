import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import AddIcon from '@mui/icons-material/Add';
import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import AddNote from './AddNote';
import {
  Box,
  Button,
  CircularProgress,
  IconButton,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useDeleteNoteMutation, useGetNotesQuery } from '../../services/backendApi';

const USER_ID = 'd44dc55f-e08c-4db2-a918-3093f1e11848';

export default function Notes() {
  const { data: notes = [], isLoading, error } = useGetNotesQuery(USER_ID);
  const [deleteNote] = useDeleteNoteMutation();
  const [addModalOpen, setAddModalOpen] = useState(false);
  const [editingNoteId, setEditingNoteId] = useState<string | null>(null);
  const location = useLocation();
  const navigate = useNavigate();
  const isOpenFromNav = (location.state as { openAddModal?: boolean })?.openAddModal;
  const modalOpen = addModalOpen || !!editingNoteId || !!isOpenFromNav;

  const handleCloseNoteModal = () => {
    setAddModalOpen(false);
    setEditingNoteId(null);
    if (isOpenFromNav) navigate('/', { replace: true, state: {} });
  };

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight={200}>
        <CircularProgress />
      </Box>
    );
  }
  if (error) return <Typography color="error">Failed to load notes</Typography>;

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 2 }}>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => {
            setEditingNoteId(null);
            setAddModalOpen(true);
          }}
        >
          Add Note
        </Button>
      </Box>
      <AddNote
        open={modalOpen}
        onClose={handleCloseNoteModal}
        userId={USER_ID}
        noteID={editingNoteId ?? undefined}
      />
      <TableContainer component={Paper}>
      <Table sx={{ minWidth: 900 }} aria-label="notes table">
        <TableHead>
          <TableRow>
            <TableCell>Title</TableCell>
            <TableCell>Content</TableCell>
            <TableCell align="right">Created</TableCell>
            <TableCell align="right">Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {notes.map((note) => (
            <TableRow key={note.id} sx={{ '&:last-child td, &:last-child th': { border: 0 } }}>
              <TableCell component="th" scope="row">
                {note.title}
              </TableCell>
              <TableCell>{note.summary}</TableCell>
              <TableCell align="right">{new Date(note.createdAt).toLocaleDateString()}</TableCell>
              <TableCell align="right">
                <IconButton
                  color="primary"
                  size="small"
                  onClick={() => {
                    setEditingNoteId(note.id);
                    setAddModalOpen(true);
                  }}
                  aria-label="edit"
                >
                  <EditIcon />
                </IconButton>
                <IconButton
                  color="error"
                  size="small"
                  onClick={() => {
                    if (window.confirm('Are you sure?')) {
                      deleteNote({ id: note.id, userId: USER_ID });
                    }
                  }}
                  aria-label="delete"
                >
                  <DeleteIcon />
                </IconButton>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      </TableContainer>
    </>
  );
}
