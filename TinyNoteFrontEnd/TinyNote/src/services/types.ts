export interface Note {
  id: string;
  userId: string;
  title: string;
  content: string;
  summary?: string;
  createdAt: string;
  updateAt: string;
}

export interface CreateNoteRequest {
  userId: string;
  title: string;
  content: string;
  summary?: string;
}
