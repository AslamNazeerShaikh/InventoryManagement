import {
  BarChart3,
  Boxes,
  ClipboardList,
  LayoutDashboard,
  UserCircle,
  Users,
  type LucideIcon,
} from "lucide-react";

export interface NavPermissions {
  canManageUsers: boolean;
}

export interface NavItem {
  label: string;
  href: string;
  icon: LucideIcon;
  visible: (perms: NavPermissions) => boolean;
}

export const navItems: NavItem[] = [
  { label: "Dashboard", href: "/dashboard", icon: LayoutDashboard, visible: () => true },
  { label: "Inventory", href: "/inventory", icon: Boxes, visible: () => true },
  { label: "Assignments", href: "/assignments", icon: ClipboardList, visible: () => true },
  { label: "Reports", href: "/reports", icon: BarChart3, visible: () => true },
  { label: "Users", href: "/users", icon: Users, visible: (p) => p.canManageUsers },
  { label: "Profile", href: "/profile", icon: UserCircle, visible: () => true },
];
