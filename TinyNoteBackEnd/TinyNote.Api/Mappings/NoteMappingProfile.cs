using AutoMapper;
using TinyNote.Api.Data.Entities;
using TinyNote.Api.DTOs;

namespace TinyNote.Api.Mappings;

public class NoteMappingProfile : Profile
{
    public NoteMappingProfile()
    {
        CreateMap<Note, NoteResponse>();
    }
}
