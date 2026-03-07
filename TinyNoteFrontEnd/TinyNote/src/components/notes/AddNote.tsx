import { useState } from 'react';
import { toast } from 'react-toastify';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
} from '@mui/material';
import { useAddNoteMutation } from '../../services/backendApi';

interface AddNoteProps {
  open: boolean;
  onClose: () => void;
  userId: "c11d7689-a680-4cd7-be95-5dfd99653dd6";
}

export default function AddNote({ open, onClose, userId }: AddNoteProps) {
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  
  const [addNote, { isLoading }] = useAddNoteMutation();

  const handleClose = () => {
    setTitle('');
    setContent('');
    
    onClose();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !content.trim()) {
      toast.error('Title and content are required');
      return;
    }
    try {
      await addNote({
        userId,
        title: title.trim(),
        content: content.trim()
      }).unwrap();
      toast.success('Note added successfully');
      handleClose();
    } catch {
      toast.error('Failed to add note');
    }
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle>Add a Note</DialogTitle>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <TextField
            autoFocus
            label="Title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
            fullWidth
          />
          <TextField
            label="Content"
            value={content}
            onChange={(e) => setContent(e.target.value)}
            required
            fullWidth
            multiline
            rows={4}
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={handleClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={isLoading}>
            {isLoading ? 'Adding...' : 'Add Note'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
