using CQRS.Core.Domain;
using CQRS.Core.Events;
using Post.Common.Events;

namespace Post.Cmd.Domain.Aggregates
{
	public class PostAggregate : AggregateRoot
	{
		private bool _active;
		private string _author;
		private readonly Dictionary<Guid, Tuple<string, string>> _comments = [];

		public bool Active
		{
			get => _active;
			set => _active = value;
		}

		public PostAggregate() { }

		public PostAggregate(Guid id, string author, string message)
		{
			RaiseEvent(new PostCreatedEvent
			{
				Id = id,
				Author = author,
				Message = message,
				DatePosted = DateTime.Now
			});
		}

		public void EditMessage(string message)
		{
			EnsureActive();
			EnsureNotEmpty(message, nameof(message));

			RaiseEvent(new MessageUpdatedEvent
			{
				Id = _id,
				Message = message
			});
		}

		public void LikePost()
		{
			EnsureActive();

			RaiseEvent(new PostLikedEvent { Id = _id });
		}

		public void AddComment(string comment, string username)
		{
			EnsureActive();
			EnsureNotEmpty(comment, nameof(comment));

			RaiseEvent(new CommentAddedEvent
			{
				Id = _id,
				CommentId = Guid.NewGuid(),
				Comment = comment,
				Username = username,
				CommentDates = DateTime.Now
			});
		}

		public void EditComment(Guid commentId, string comment, string username)
		{
			EnsureActive();
			EnsureSameUser(_comments[commentId].Item2, username,
				"You are not allowed to edit a comment that was made by another user!");

			RaiseEvent(new CommentUpdatedEvent
			{
				Id = _id,
				CommentId = commentId,
				Comment = comment,
				Username = username,
				EditDate = DateTime.Now
			});
		}

		public void RemoveComment(Guid commentId, string username)
		{
			EnsureActive();
			EnsureSameUser(_comments[commentId].Item2, username,
				"You are not allowed to remove a comment that was made by another user!");

			RaiseEvent(new CommentRemovedEvent { Id = _id, CommentId = commentId });
		}

		public void DeletePost(string username)
		{
			EnsureActive();
			EnsureSameUser(_author, username,
				"You are not allowed to delete a post that was made by someone else!");

			RaiseEvent(new PostRemovedEvent { Id = _id });
		}

		public void Apply(BaseEvent @event)
		{
			switch (@event)
			{
				case PostCreatedEvent e:
					SetId(e.Id);
					_active = true;
					_author = e.Author;
					break;

				case MessageUpdatedEvent e:
					SetId(e.Id);
					break;

				case PostLikedEvent e:
					SetId(e.Id);
					break;

				case CommentAddedEvent e:
					SetId(e.Id);
					_comments.Add(e.CommentId, Tuple.Create(e.Comment, e.Username));
					break;

				case CommentUpdatedEvent e:
					SetId(e.Id);
					_comments[e.CommentId] = Tuple.Create(e.Comment, e.Username);
					break;

				case CommentRemovedEvent e:
					SetId(e.Id);
					_comments.Remove(e.CommentId);
					break;

				case PostRemovedEvent e:
					SetId(e.Id);
					_active = false;
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(@event),
						$"No handler defined for {@event.GetType().Name}");
			}
		}

		#region Auxiliar Methods
		
		private void EnsureActive()
		{
			if (!_active)
				throw new InvalidOperationException("Operation not allowed on inactive post!");
		}

		private void EnsureNotEmpty(string value, string paramName)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new InvalidOperationException($"The value of {paramName} cannot be null or empty.");
		}

		private void EnsureSameUser(string expected, string actual, string errorMessage)
		{
			if (!expected.Equals(actual, StringComparison.CurrentCultureIgnoreCase))
				throw new InvalidOperationException(errorMessage);
		}

		private void SetId(Guid id) => _id = id;

		#endregion
	}
}