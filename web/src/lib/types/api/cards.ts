import type { Tag } from "./tags";
import type { SimplifiedUser, SimplifiedUserResponse } from "./users";

export interface CardResponse {
  id: string;
  listId: string;
  swimlaneId: string;
  boardId: string;
  title: string;
  color: string;
  description: string | null;
  tags: Tag[];
  assignedUsers: SimplifiedUserResponse[];
  lockedByUser: SimplifiedUserResponse | null;
  dueDate: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface Card {
  id: string;
  listId: string;
  swimlaneId: string;
  boardId: string;
  title: string;
  color: string;
  description: string | null;
  tags: Tag[]
  assignedUsers: SimplifiedUser[];
  lockedByUser: SimplifiedUser | null;
  dueDate: Date | null;
  createdAt: Date;
  updatedAt: Date;
}

export function parseCard(raw: CardResponse): Card {
  return {
    ...raw,
    dueDate: raw.dueDate ? new Date(raw.dueDate) : null,
    createdAt: new Date(raw.createdAt),
    updatedAt: new Date(raw.updatedAt),
  };
}

export interface CreateCardRequest {
  swimlaneId: string;
  listId: string;
  title: string;
  tags: string[] | null;
  dueDate: string | null;
  assignedUsers: string[] | null;
  description: string | null;
}

export type UpdateCardRequest = Omit<CreateCardRequest, 'swimlaneId' | 'listId'>