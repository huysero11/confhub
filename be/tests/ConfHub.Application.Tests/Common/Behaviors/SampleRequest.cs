using MediatR;

namespace ConfHub.Application.Tests.Common.Behaviors;

// Request mẫu chỉ dùng trong test, để thử ValidationBehavior mà không cần use case thật.
public sealed record SampleRequest(string Title) : IRequest<string>;
