export interface BoardDto {
  id: string;
  name: string;
  ownerId: string;
  color: string;
  taskCount: number;
  createdAtUtc: string;
}

export interface CreateBoardRequest {
  name: string;
  color?: string;
}

export enum BoardRole {
  Owner = 'Owner',
  Member = 'Member'
}

export interface BoardMemberDto {
  userId: string;
  displayName: string;
  email: string;
  color: string;
  role: BoardRole;
}

export interface AddBoardMemberRequest {
  userId: string;
  role: BoardRole;
}
