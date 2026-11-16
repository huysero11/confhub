using ConfHub.Api.Authorization;
using ConfHub.Api.Controllers.Manage.Requests;
using ConfHub.Application.Common;
using ConfHub.Application.Rooms;
using ConfHub.Application.Rooms.CreateRoom;
using ConfHub.Application.Rooms.DeleteRoom;
using ConfHub.Application.Rooms.GetRooms;
using ConfHub.Application.Rooms.UpdateRoom;
using ConfHub.Domain.Accounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ConfHub.Api.Controllers.Manage;

[ApiController]
[Route("api/manage/conferences/{conferenceId:guid}/rooms")]
[MustHavePermission(Permissions.RoomManage)]
public sealed class ManageRoomsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<RoomResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRooms(
        Guid conferenceId,
        CancellationToken cancellationToken)
    {
        var query = new GetRoomsQuery(conferenceId);
        var response = await sender.Send(query, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<CreatedIdResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRoom(
        Guid conferenceId,
        RoomRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRoomCommand(conferenceId, request.Name, request.Capacity);
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetRooms), new { conferenceId }, response);
    }

    [HttpPut("{roomId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateRoom(
        Guid conferenceId,
        Guid roomId,
        RoomRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoomCommand(conferenceId, roomId, request.Name, request.Capacity);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{roomId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRoom(
        Guid conferenceId,
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRoomCommand(conferenceId, roomId);
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
