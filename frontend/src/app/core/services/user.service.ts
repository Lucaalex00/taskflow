import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserDto } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly baseUrl = `${environment.apiUrl}/users`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Promise<UserDto[]> {
    return firstValueFrom(this.http.get<UserDto[]>(this.baseUrl));
  }
}
