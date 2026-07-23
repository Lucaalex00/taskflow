export interface BoardDto {
  id: string;
  name: string;
  ownerId: string;
  taskCount: number;
  createdAtUtc: string;
}

export interface CreateBoardRequest {
  name: string;
  ownerId: string;
}
