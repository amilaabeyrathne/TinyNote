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
  id: string;
  userId: string;
  title: string;
  content: string;
  summary?: string;
}
