export interface Note {
  id: string;
  title: string;
  content: string;
  summary?: string;
  createdAt: string;
}

export interface CreateNoteRequest {
  userId: string;
  title: string;
  content: string;
  summary?: string;
}

export interface UpdateNoteRequest {
  userId: string;
  title: string;
  content: string;
}

export interface GetNotesParams {
  userId: string;
  search?: string;
  sortBy?: 'title' | 'createdAt';
  sortOrder?: 'asc' | 'desc';
}
