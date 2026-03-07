import {
  Box,
  CircularProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useGetNotesQuery } from '../../services/backendApi';

export default function Notes() {
  const { data: notes = [], isLoading, error } = useGetNotesQuery('c11d7689-a680-4cd7-be95-5dfd99653dd6');

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight={200}>
        <CircularProgress />
      </Box>
    );
  }
  if (error) return <Typography color="error">Failed to load notes</Typography>;
  if (notes.length === 0) return <Typography>No notes yet</Typography>;

  return (
    <TableContainer component={Paper}>
      <Table sx={{ minWidth: 900 }} aria-label="notes table">
        <TableHead>
          <TableRow>
            <TableCell>Title</TableCell>
            <TableCell>Content</TableCell>
            <TableCell align="right">Created</TableCell>
            <TableCell align="right">Updated</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {notes.map((note) => (
            <TableRow key={note.id} sx={{ '&:last-child td, &:last-child th': { border: 0 } }}>
              <TableCell component="th" scope="row">
                {note.title}
              </TableCell>
              <TableCell>{note.summary ?? note.content}</TableCell>
              <TableCell align="right">{new Date(note.createdAt).toLocaleDateString()}</TableCell>
              <TableCell align="right">{new Date(note.updateAt).toLocaleDateString()}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
