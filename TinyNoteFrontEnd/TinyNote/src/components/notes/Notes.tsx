import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import AddIcon from '@mui/icons-material/Add';
import { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import AddNote from './AddNote';
import SearchIcon from '@mui/icons-material/Search';
import {
  Box,
  Button,
  CircularProgress,
  FormControl,
  IconButton,
  InputAdornment,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Table,
  TextField,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useDeleteNoteMutation, useGetNotesQuery } from '../../services/backendApi';
import type { GetNotesParams } from '../../services/types';

const USER_ID = 'd44dc55f-e08c-4db2-a918-3093f1e11848';

const SEARCH_DEBOUNCE_MS = 300;

export default function Notes() {
  const [sortBy, setSortBy] = useState<GetNotesParams['sortBy']>('createdAt');
  const [sortOrder, setSortOrder] = useState<GetNotesParams['sortOrder']>('desc');
  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchInput.trim()), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const { data: notes = [], isLoading, error } = useGetNotesQuery({
    userId: USER_ID,
    sortBy,
    sortOrder,
    ...(debouncedSearch && { search: debouncedSearch }),
  });
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
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, gap: 2, flexWrap: 'wrap' }}>
        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
          <TextField
            size="small"
            placeholder="Search notes…"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon color="action" />
                </InputAdornment>
              ),
            }}
            sx={{ minWidth: 200 }}
          />
          <FormControl size="small" sx={{ minWidth: 120 }}>
            <InputLabel>Sort by</InputLabel>
            <Select
              value={sortBy}
              label="Sort by"
              onChange={(e) => setSortBy(e.target.value as GetNotesParams['sortBy'])}
            >
              <MenuItem value="createdAt">Date</MenuItem>
              <MenuItem value="title">Title</MenuItem>
            </Select>
          </FormControl>
          <FormControl size="small" sx={{ minWidth: 100 }}>
            <InputLabel>Order</InputLabel>
            <Select
              value={sortOrder}
              label="Order"
              onChange={(e) => setSortOrder(e.target.value as GetNotesParams['sortOrder'])}
            >
              <MenuItem value="desc">Descending</MenuItem>
              <MenuItem value="asc">Ascending</MenuItem>
            </Select>
          </FormControl>
        </Box>
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
