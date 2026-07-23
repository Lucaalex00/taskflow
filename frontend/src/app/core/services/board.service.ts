import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BoardDto, CreateBoardRequest } from '../models/board.model';

@Injectable({ providedIn: 'root' })
export class BoardService {
  private readonly baseUrl = `${environment.apiUrl}/boards`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Promise<BoardDto[]> {
    return firstValueFrom(this.http.get<BoardDto[]>(this.baseUrl));
  }

  create(request: CreateBoardRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>(this.baseUrl, request));
  }
}
