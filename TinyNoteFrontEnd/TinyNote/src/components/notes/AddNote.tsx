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

const TITLE_MAX_LENGTH = 100;
const CONTENT_MAX_LENGTH = 2000;

interface AddNoteProps {
  open: boolean;
  onClose: () => void;
  userId: "d44dc55f-e08c-4db2-a918-3093f1e11848";
}

export default function AddNote({ open, onClose, userId }: AddNoteProps) {
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [errors, setErrors] = useState<{ title?: string; content?: string }>({});
  const [addNote, { isLoading }] = useAddNoteMutation();

  const handleClose = () => {
    setTitle('');
    setContent('');
    setErrors({});
    onClose();
  };

  const validate = (): boolean => {
    const newErrors: { title?: string; content?: string } = {};
    const trimmedTitle = title.trim();
    const trimmedContent = content.trim();

    if (!trimmedTitle) {
      newErrors.title = 'Title is required';
    } else if (trimmedTitle.length > TITLE_MAX_LENGTH) {
      newErrors.title = `Title cannot exceed ${TITLE_MAX_LENGTH} characters`;
    }

    if (!trimmedContent) {
      newErrors.content = 'Content is required';
    } else if (trimmedContent.length > CONTENT_MAX_LENGTH) {
      newErrors.content = `Content cannot exceed ${CONTENT_MAX_LENGTH} characters`;
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    const trimmedTitle = title.trim();
    const trimmedContent = content.trim();

    try {
      await addNote({
        userId,
        title: trimmedTitle,
        content: trimmedContent,
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
            onChange={(e) => {
              setTitle(e.target.value);
              if (errors.title) setErrors((prev) => ({ ...prev, title: undefined }));
            }}
            error={!!errors.title}
            helperText={errors.title ?? `${title.length}/${TITLE_MAX_LENGTH}`}
            inputProps={{ maxLength: TITLE_MAX_LENGTH }}
            fullWidth
            required
          />
          <TextField
            label="Content"
            value={content}
            onChange={(e) => {
              setContent(e.target.value);
              if (errors.content) setErrors((prev) => ({ ...prev, content: undefined }));
            }}
            error={!!errors.content}
            helperText={errors.content ?? `${content.length}/${CONTENT_MAX_LENGTH}`}
            inputProps={{ maxLength: CONTENT_MAX_LENGTH }}
            fullWidth
            multiline
            rows={4}
            required
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
