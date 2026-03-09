using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TinyNote.Api.Data.Entities;
using TinyNote.Api.DTOs;
using TinyNote.Api.Exceptions;
using TinyNote.Api.Metrics;
using TinyNote.Api.Repository;
using TinyNote.Api.Services;

namespace TinyNote.Tests.Services;

public class NotesServiceTests : IDisposable
{
    private readonly Mock<INoteRepository> _noteRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly TinyNoteMetrics _metrics;
    private readonly NotesService _sut;

    public NotesServiceTests()
    {
        _noteRepositoryMock = new Mock<INoteRepository>();
        _mapperMock = new Mock<IMapper>();
        _metrics = new TinyNoteMetrics();

        _sut = new NotesService(
            _noteRepositoryMock.Object,
            _mapperMock.Object,
            NullLogger<NotesService>.Instance,
            _metrics);
    }

    public void Dispose() => _metrics.Dispose();

    [Fact]
    public async Task AddNoteAsync_WithValidRequest_ReturnsMappedNoteResponse()
    {
        var userId = Guid.NewGuid();
        var request = new CreateNoteRequest
        {
            UserId = userId,
            Title = "Test Title",
            Content = "Test Content"
        };

        var savedNote = new Note
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Content = request.Content,
            Summary = request.Content,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdateAt = DateTimeOffset.UtcNow
        };

        var expectedResponse = new NoteResponse
        {
            Id = savedNote.Id,
            Title = savedNote.Title,
            Content = savedNote.Content,
            Summary = savedNote.Summary,
            CreatedAt = savedNote.CreatedAt
        };

        _noteRepositoryMock
            .Setup(r => r.AddNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedNote);

        _mapperMock
            .Setup(m => m.Map<NoteResponse>(savedNote))
            .Returns(expectedResponse);

        var result = await _sut.AddNoteAsync(request);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task AddNoteAsync_PassesCorrectNotePropertiesToRepository()
    {
        var userId = Guid.NewGuid();
        var request = new CreateNoteRequest
        {
            UserId = userId,
            Title = "My Title",
            Content = "My Content"
        };

        Note? capturedNote = null;

        _noteRepositoryMock
            .Setup(r => r.AddNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .Callback<Note, CancellationToken>((note, _) => capturedNote = note)
            .ReturnsAsync(new Note { Id = Guid.NewGuid(), Title = request.Title, Content = request.Content });

        _mapperMock
            .Setup(m => m.Map<NoteResponse>(It.IsAny<Note>()))
            .Returns(new NoteResponse());

        await _sut.AddNoteAsync(request);

        capturedNote.Should().NotBeNull();
        capturedNote!.UserId.Should().Be(userId);
        capturedNote.Title.Should().Be(request.Title);
        capturedNote.Content.Should().Be(request.Content);
        capturedNote.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        capturedNote.UpdateAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AddNoteAsync_ContentLongerThan50Chars_SummaryIsTruncatedTo50CharsWithEllipsis()
    {
        var longContent = new string('x', 60);
        var request = new CreateNoteRequest
        {
            UserId = Guid.NewGuid(),
            Title = "Title",
            Content = longContent
        };

        Note? capturedNote = null;

        _noteRepositoryMock
            .Setup(r => r.AddNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .Callback<Note, CancellationToken>((note, _) => capturedNote = note)
            .ReturnsAsync(new Note());

        _mapperMock
            .Setup(m => m.Map<NoteResponse>(It.IsAny<Note>()))
            .Returns(new NoteResponse());

        await _sut.AddNoteAsync(request);

        capturedNote!.Summary.Should().Be(new string('x', 50) + "...");
    }

    [Fact]
    public async Task AddNoteAsync_ContentShorterThanOrEqualTo50Chars_SummaryEqualsContent()
    {
        var shortContent = "Short content";
        var request = new CreateNoteRequest
        {
            UserId = Guid.NewGuid(),
            Title = "Title",
            Content = shortContent
        };

        Note? capturedNote = null;

        _noteRepositoryMock
            .Setup(r => r.AddNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .Callback<Note, CancellationToken>((note, _) => capturedNote = note)
            .ReturnsAsync(new Note());

        _mapperMock
            .Setup(m => m.Map<NoteResponse>(It.IsAny<Note>()))
            .Returns(new NoteResponse());

        await _sut.AddNoteAsync(request);

        capturedNote!.Summary.Should().Be(shortContent);
    }

    [Fact]
    public async Task AddNoteAsync_ContentExactly50Chars_SummaryIsNotTruncated()
    {
        var exactContent = new string('x', 50);
        var request = new CreateNoteRequest
        {
            UserId = Guid.NewGuid(),
            Title = "Title",
            Content = exactContent
        };

        Note? capturedNote = null;

        _noteRepositoryMock
            .Setup(r => r.AddNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .Callback<Note, CancellationToken>((note, _) => capturedNote = note)
            .ReturnsAsync(new Note());

        _mapperMock
            .Setup(m => m.Map<NoteResponse>(It.IsAny<Note>()))
            .Returns(new NoteResponse());

        await _sut.AddNoteAsync(request);

        capturedNote!.Summary.Should().Be(exactContent);
        capturedNote.Summary.Should().HaveLength(50);
    }

    [Fact]
    public async Task AddNoteAsync_ContentExactly51Chars_SummaryIsTruncated()
    {
        var contentWith51Chars = new string('x', 51);
        var request = new CreateNoteRequest
        {
            UserId = Guid.NewGuid(),
            Title = "Title",
            Content = contentWith51Chars
        };

        Note? capturedNote = null;

        _noteRepositoryMock
            .Setup(r => r.AddNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .Callback<Note, CancellationToken>((note, _) => capturedNote = note)
            .ReturnsAsync(new Note());

        _mapperMock
            .Setup(m => m.Map<NoteResponse>(It.IsAny<Note>()))
            .Returns(new NoteResponse());

        await _sut.AddNoteAsync(request);

        capturedNote!.Summary.Should().Be(new string('x', 50) + "...");
        capturedNote.Summary.Should().HaveLength(53);
    }

    [Fact]
    public async Task AddNoteAsync_CallsRepositoryExactlyOnce()
    {
        var request = new CreateNoteRequest
        {
            UserId = Guid.NewGuid(),
            Title = "Title",
            Content = "Content"
        };

        _noteRepositoryMock
            .Setup(r => r.AddNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Note());

        _mapperMock
            .Setup(m => m.Map<NoteResponse>(It.IsAny<Note>()))
            .Returns(new NoteResponse());

        await _sut.AddNoteAsync(request);

        _noteRepositoryMock.Verify(
            r => r.AddNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateNoteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateNoteAsync_WithValidRequest_ReturnsMappedNoteResponse()
    {
        var noteId = Guid.NewGuid();
        var request = new UpdateNoteRequest
        {
            Id = noteId,
            UserId = Guid.NewGuid(),
            Title = "Updated Title",
            Content = "Updated Content"
        };

        var existingNote = new Note
        {
            Id = noteId,
            Title = "Old Title",
            Content = "Old Content",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var updatedNote = new Note
        {
            Id = noteId,
            Title = request.Title,
            Content = request.Content
        };

        var expectedResponse = new NoteResponse
        {
            Id = noteId,
            Title = request.Title,
            Content = request.Content
        };

        _noteRepositoryMock
            .Setup(r => r.GetNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingNote);

        _noteRepositoryMock
            .Setup(r => r.UpdateNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedNote);

        _mapperMock
            .Setup(m => m.Map<NoteResponse>(updatedNote))
            .Returns(expectedResponse);

        var result = await _sut.UpdateNoteAsync(request);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateNoteAsync_NoteNotFound_ThrowsItemNotFoundException()
    {
        var noteId = Guid.NewGuid();
        var request = new UpdateNoteRequest
        {
            Id = noteId,
            UserId = Guid.NewGuid(),
            Title = "Title",
            Content = "Content"
        };

        _noteRepositoryMock
            .Setup(r => r.GetNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Note?)null);

        var act = async () => await _sut.UpdateNoteAsync(request);

        await act.Should().ThrowAsync<ItemNotFoundException>()
            .WithMessage($"*{noteId}*");
    }

    [Fact]
    public async Task UpdateNoteAsync_NoteNotFound_NeverCallsUpdateRepository()
    {
        var noteId = Guid.NewGuid();
        var request = new UpdateNoteRequest
        {
            Id = noteId,
            UserId = Guid.NewGuid(),
            Title = "Title",
            Content = "Content"
        };

        _noteRepositoryMock
            .Setup(r => r.GetNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Note?)null);

        await Assert.ThrowsAsync<ItemNotFoundException>(() => _sut.UpdateNoteAsync(request));

        _noteRepositoryMock.Verify(
            r => r.UpdateNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateNoteAsync_UpdatesExistingNoteFieldsBeforePassingToRepository()
    {
        var noteId = Guid.NewGuid();
        var request = new UpdateNoteRequest
        {
            Id = noteId,
            UserId = Guid.NewGuid(),
            Title = "New Title",
            Content = "New Content"
        };

        var existingNote = new Note
        {
            Id = noteId,
            Title = "Old Title",
            Content = "Old Content",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        Note? capturedNote = null;

        _noteRepositoryMock
            .Setup(r => r.GetNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingNote);

        _noteRepositoryMock
            .Setup(r => r.UpdateNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .Callback<Note, CancellationToken>((note, _) => capturedNote = note)
            .ReturnsAsync(new Note());

        _mapperMock
            .Setup(m => m.Map<NoteResponse>(It.IsAny<Note>()))
            .Returns(new NoteResponse());

        await _sut.UpdateNoteAsync(request);

        capturedNote.Should().NotBeNull();
        capturedNote!.Title.Should().Be(request.Title);
        capturedNote.Content.Should().Be(request.Content);
        capturedNote.UpdateAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateNoteAsync_ContentLongerThan50Chars_SummaryIsTruncated()
    {
        var noteId = Guid.NewGuid();
        var longContent = new string('a', 60);
        var request = new UpdateNoteRequest
        {
            Id = noteId,
            UserId = Guid.NewGuid(),
            Title = "Title",
            Content = longContent
        };

        Note? capturedNote = null;

        _noteRepositoryMock
            .Setup(r => r.GetNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Note { Id = noteId });

        _noteRepositoryMock
            .Setup(r => r.UpdateNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .Callback<Note, CancellationToken>((note, _) => capturedNote = note)
            .ReturnsAsync(new Note());

        _mapperMock
            .Setup(m => m.Map<NoteResponse>(It.IsAny<Note>()))
            .Returns(new NoteResponse());

        await _sut.UpdateNoteAsync(request);

        capturedNote!.Summary.Should().Be(new string('a', 50) + "...");
    }

    [Fact]
    public async Task UpdateNoteAsync_ContentShorterThanOrEqualTo50Chars_SummaryEqualsContent()
    {
        var noteId = Guid.NewGuid();
        var shortContent = "Short content";
        var request = new UpdateNoteRequest
        {
            Id = noteId,
            UserId = Guid.NewGuid(),
            Title = "Title",
            Content = shortContent
        };

        Note? capturedNote = null;

        _noteRepositoryMock
            .Setup(r => r.GetNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Note { Id = noteId });

        _noteRepositoryMock
            .Setup(r => r.UpdateNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .Callback<Note, CancellationToken>((note, _) => capturedNote = note)
            .ReturnsAsync(new Note());

        _mapperMock
            .Setup(m => m.Map<NoteResponse>(It.IsAny<Note>()))
            .Returns(new NoteResponse());

        await _sut.UpdateNoteAsync(request);

        capturedNote!.Summary.Should().Be(shortContent);
    }
}
