import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { Note, CreateNoteRequest, UpdateNoteRequest, GetNotesParams } from './types';

export const backendApi = createApi({
  reducerPath: 'backendApi',
  baseQuery: fetchBaseQuery({
    baseUrl: '/api',
  }),
  tagTypes: ['Note'],
  endpoints: (builder) => ({
    getNotes: builder.query<Note[], GetNotesParams>({
      query: ({ userId, search, sortBy = 'createdAt', sortOrder = 'desc' }) => ({
        url: 'notes',
        params: { userId, ...(search && { search }), sortBy, sortOrder },
      }),
      providesTags: (result, _error, { userId }) =>
        result
          ? [
              ...result.map(({ id }) => ({ type: 'Note' as const, id })),
              { type: 'Note', id: `LIST-${userId}` },
            ]
          : [{ type: 'Note', id: `LIST-${userId}` }],
    }),
    getNote: builder.query<Note, string>({
      query: (id) => `notes/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Note', id }],
    }),
    addNote: builder.mutation<Note, CreateNoteRequest>({
      query: (body) => ({
        url: 'notes',
        method: 'POST',
        body,
      }),
      invalidatesTags: (_result, _error, { userId }) => [
        { type: 'Note', id: `LIST-${userId}` },
      ],
    }),
    updateNote: builder.mutation<Note, UpdateNoteRequest>({
      query: (body) => ({
        url: 'notes',
        method: 'PUT',
        body,
      }),
      invalidatesTags: (_result, _error, { id, userId }) => [
        { type: 'Note', id },
        { type: 'Note', id: `LIST-${userId}` },
      ],
    }),
    deleteNote: builder.mutation<void, { id: string; userId: string }>({
      query: ({ id }) => ({
        url: `notes/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, { id, userId }) => [
        { type: 'Note', id },
        { type: 'Note', id: `LIST-${userId}` },
      ],
    }),
  }),
});

export const {
  useGetNotesQuery,
  useGetNoteQuery,
  useAddNoteMutation,
  useUpdateNoteMutation,
  useDeleteNoteMutation,
} = backendApi;
