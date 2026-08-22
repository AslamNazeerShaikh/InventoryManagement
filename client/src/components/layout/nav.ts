import {
  BarChart3,
  Boxes,
  ClipboardList,
  LayoutDashboard,
  MapPin,
  Palette,
  Truck,
  UserCircle,
  Users,
  Wrench,
  type LucideIcon,
} from "lucide-react";

export interface NavPermissions {
  canManageUsers: boolean;
  canManage: boolean;
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
  { label: "Suppliers", href: "/suppliers", icon: Truck, visible: (p) => p.canManage },
  { label: "Locations", href: "/locations", icon: MapPin, visible: (p) => p.canManage },
  { label: "Maintenance", href: "/maintenance", icon: Wrench, visible: (p) => p.canManage },
  { label: "Reports", href: "/reports", icon: BarChart3, visible: () => true },
  { label: "Users", href: "/users", icon: Users, visible: (p) => p.canManageUsers },
  { label: "Profile", href: "/profile", icon: UserCircle, visible: () => true },
  { label: "Appearance", href: "/appearance", icon: Palette, visible: () => true },
];
