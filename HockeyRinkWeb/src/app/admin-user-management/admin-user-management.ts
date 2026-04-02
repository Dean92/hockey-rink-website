import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import {
  AdminService,
  AdminUserSummary,
  UserSearchResult,
  ALL_PERMISSIONS,
} from '../admin.service';

@Component({
  selector: 'app-admin-user-management',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-user-management.html',
  styleUrl: './admin-user-management.css',
})
export class AdminUserManagement implements OnInit {
  activeTab = signal<'admins' | 'add'>('admins');

  // ── Current admins tab ─────────────────────────────────────────────────────
  adminUsers = signal<AdminUserSummary[]>([]);
  isLoadingAdmins = signal(false);
  savingUserId = signal<string | null>(null);
  revokingUserId = signal<string | null>(null);

  // ── Add/Invite tab ─────────────────────────────────────────────────────────
  searchQuery = signal('');
  searchResults = signal<UserSearchResult[]>([]);
  isSearching = signal(false);
  selectedUser = signal<UserSearchResult | null>(null);
  inviteEmail = signal('');
  addMode = signal<'search' | 'invite'>('search');
  newPermissions = signal<Record<string, boolean>>({});
  isSubmitting = signal(false);

  // ── Messages ───────────────────────────────────────────────────────────────
  successMessage = signal<string | null>(null);
  errorMessage = signal<string | null>(null);

  readonly allPermissions = ALL_PERMISSIONS;

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadAdmins();
    this.resetNewPermissions();
  }

  loadAdmins(): void {
    this.isLoadingAdmins.set(true);
    this.adminService.getAdminUsers().subscribe({
      next: (users) => {
        this.adminUsers.set(users);
        this.isLoadingAdmins.set(false);
      },
      error: (err) => {
        this.errorMessage.set(
          err.error?.message ?? 'Failed to load admin users',
        );
        this.isLoadingAdmins.set(false);
      },
    });
  }

  hasPermission(user: AdminUserSummary, perm: string): boolean {
    return user.permissions.includes(perm);
  }

  togglePermission(
    user: AdminUserSummary,
    perm: string,
    checked: boolean,
  ): void {
    if (checked) {
      user.permissions = [...user.permissions, perm];
    } else {
      user.permissions = user.permissions.filter((p) => p !== perm);
    }
  }

  savePermissions(user: AdminUserSummary): void {
    this.savingUserId.set(user.id);
    this.errorMessage.set(null);

    this.adminService
      .updateAdminPermissions(user.id, user.permissions)
      .subscribe({
        next: () => {
          this.successMessage.set(`Permissions updated for ${user.email}`);
          this.savingUserId.set(null);
          setTimeout(() => this.successMessage.set(null), 3000);
        },
        error: (err) => {
          this.errorMessage.set(
            err.error?.message ?? 'Failed to update permissions',
          );
          this.savingUserId.set(null);
        },
      });
  }

  // ── Revoke confirmation modal ──────────────────────────────────────────────
  showRevokeModal = signal(false);
  userToRevoke = signal<AdminUserSummary | null>(null);

  openRevokeModal(user: AdminUserSummary): void {
    this.userToRevoke.set(user);
    this.showRevokeModal.set(true);
  }

  cancelRevoke(): void {
    this.showRevokeModal.set(false);
    this.userToRevoke.set(null);
  }

  revokeAdmin(user: AdminUserSummary): void {
    this.revokingUserId.set(user.id);
    this.showRevokeModal.set(false);
    this.errorMessage.set(null);

    this.adminService.revokeAdmin(user.id).subscribe({
      next: () => {
        this.successMessage.set(`Admin access revoked for ${user.email}`);
        this.revokingUserId.set(null);
        this.userToRevoke.set(null);
        this.loadAdmins();
        setTimeout(() => this.successMessage.set(null), 3000);
      },
      error: (err) => {
        this.errorMessage.set(
          err.error?.message ?? 'Failed to revoke admin access',
        );
        this.revokingUserId.set(null);
        this.userToRevoke.set(null);
      },
    });
  }

  // ── Add/Invite tab ─────────────────────────────────────────────────────────

  searchUsers(): void {
    const q = this.searchQuery().trim();
    if (!q) {
      this.searchResults.set([]);
      return;
    }

    this.isSearching.set(true);
    this.adminService.getAllUsersForSearch(q).subscribe({
      next: (results) => {
        this.searchResults.set(results);
        this.isSearching.set(false);
      },
      error: () => {
        this.isSearching.set(false);
      },
    });
  }

  selectUser(user: UserSearchResult): void {
    this.selectedUser.set(user);
    this.searchResults.set([]);
    this.searchQuery.set(
      `${user.firstName ?? ''} ${user.lastName ?? ''} (${user.email})`,
    );
  }

  clearSelectedUser(): void {
    this.selectedUser.set(null);
    this.searchQuery.set('');
    this.searchResults.set([]);
  }

  resetNewPermissions(): void {
    const perms: Record<string, boolean> = {};
    ALL_PERMISSIONS.forEach((p) => (perms[p.value] = false));
    this.newPermissions.set(perms);
  }

  toggleNewPermission(perm: string, checked: boolean): void {
    this.newPermissions.update((prev) => ({ ...prev, [perm]: checked }));
  }

  get selectedPermissions(): string[] {
    return Object.entries(this.newPermissions())
      .filter(([, v]) => v)
      .map(([k]) => k);
  }

  grantAdmin(): void {
    const perms = this.selectedPermissions;
    if (perms.length === 0) {
      this.errorMessage.set('Select at least one permission');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const user = this.selectedUser();
    if (!user) return;

    this.adminService.grantAdmin(user.id, perms).subscribe({
      next: () => {
        this.successMessage.set(`Admin access granted to ${user.email}`);
        this.isSubmitting.set(false);
        this.clearSelectedUser();
        this.resetNewPermissions();
        this.loadAdmins();
        this.activeTab.set('admins');
        setTimeout(() => this.successMessage.set(null), 4000);
      },
      error: (err) => {
        this.errorMessage.set(
          err.error?.message ?? 'Failed to grant admin access',
        );
        this.isSubmitting.set(false);
      },
    });
  }

  inviteAdmin(): void {
    const email = this.inviteEmail().trim();
    if (!email) {
      this.errorMessage.set('Email is required');
      return;
    }

    const perms = this.selectedPermissions;
    if (perms.length === 0) {
      this.errorMessage.set('Select at least one permission');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.adminService.inviteAdmin(email, perms).subscribe({
      next: () => {
        this.successMessage.set(`Invitation sent to ${email}`);
        this.isSubmitting.set(false);
        this.inviteEmail.set('');
        this.resetNewPermissions();
        this.loadAdmins();
        this.activeTab.set('admins');
        setTimeout(() => this.successMessage.set(null), 4000);
      },
      error: (err) => {
        this.errorMessage.set(
          err.error?.message ?? 'Failed to send invitation',
        );
        this.isSubmitting.set(false);
      },
    });
  }

  permLabel(value: string): string {
    return ALL_PERMISSIONS.find((p) => p.value === value)?.label ?? value;
  }
}
