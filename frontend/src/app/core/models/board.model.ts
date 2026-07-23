export interface BoardDto {
  id: string;
  name: string;
  ownerId: string;
  taskCount: number;
  createdAtUtc: string;
}

export interface CreateBoardRequest {
  name: string;
}

export enum BoardRole {
  Owner = 'Owner',
  Member = 'Member'
}

export interface BoardMemberDto {
  userId: string;
  displayName: string;
  email: string;
  role: BoardRole;
}

export interface AddBoardMemberRequest {
  userId: string;
  role: BoardRole;
}
